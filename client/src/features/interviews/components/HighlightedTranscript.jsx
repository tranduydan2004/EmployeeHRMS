import { useMemo } from 'react';

/**
 * Strips leading/trailing punctuation, quotes, and whitespace from a quote string.
 */
export function cleanQuote(raw) {
  if (!raw || typeof raw !== 'string') return '';
  return raw.replace(/^["'«»“”\s,.:;!?]+|["'«»“”\s,.:;!?]+$/g, '').trim();
}

/**
 * Escapes special regex characters.
 */
function escapeRegex(str) {
  return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

/**
 * Finds non-overlapping ranges of evidence quotes within the transcript text.
 */
export function findQuoteRanges(transcript, quotes = []) {
  if (!transcript || !Array.isArray(quotes) || quotes.length === 0) return [];
  const ranges = [];

  quotes.forEach((q, quoteIdx) => {
    const cleaned = cleanQuote(q);
    if (!cleaned || cleaned.length < 3) return;

    const escaped = escapeRegex(cleaned);
    // Support flexible whitespace matching (tabs, spaces, newlines)
    const pattern = escaped.replace(/\s+/g, '\\s+');

    try {
      const regex = new RegExp(pattern, 'giu');
      let match;
      while ((match = regex.exec(transcript)) !== null) {
        // Prevent infinite loop on zero-length matches
        if (match[0].length === 0) break;

        ranges.push({
          start: match.index,
          end: match.index + match[0].length,
          quoteIndex: quoteIdx,
        });
      }
    } catch {
      // Fallback silently if complex regex fails
    }
  });

  if (ranges.length === 0) return [];

  // Sort ranges by start position, then length descending
  ranges.sort((a, b) => a.start - b.start || b.end - a.end);

  // Merge overlapping / touching ranges
  const merged = [];
  for (const range of ranges) {
    if (merged.length === 0) {
      merged.push({ ...range, quoteIndices: [range.quoteIndex] });
      continue;
    }

    const last = merged[merged.length - 1];
    if (range.start < last.end) {
      last.end = Math.max(last.end, range.end);
      if (!last.quoteIndices.includes(range.quoteIndex)) {
        last.quoteIndices.push(range.quoteIndex);
      }
    } else {
      merged.push({ ...range, quoteIndices: [range.quoteIndex] });
    }
  }

  return merged;
}

/**
 * Splits transcript into plain and highlighted segments.
 */
export function splitIntoSegments(transcript, mergedRanges) {
  if (!mergedRanges || mergedRanges.length === 0) {
    return [{ text: transcript, isHighlight: false }];
  }

  const segments = [];
  let currentPos = 0;

  for (const range of mergedRanges) {
    if (range.start > currentPos) {
      segments.push({
        text: transcript.slice(currentPos, range.start),
        isHighlight: false,
      });
    }

    segments.push({
      text: transcript.slice(range.start, range.end),
      isHighlight: true,
      quoteIndices: range.quoteIndices,
      start: range.start,
    });

    currentPos = range.end;
  }

  if (currentPos < transcript.length) {
    segments.push({
      text: transcript.slice(currentPos),
      isHighlight: false,
    });
  }

  return segments;
}

export default function HighlightedTranscript({
  transcript = '',
  evidenceQuotes = [],
  isHighlightEnabled = true,
  activeQuoteIndex = null,
}) {
  const segments = useMemo(() => {
    if (!transcript) return [];
    if (!isHighlightEnabled || !evidenceQuotes || evidenceQuotes.length === 0) {
      return [{ text: transcript, isHighlight: false }];
    }

    const ranges = findQuoteRanges(transcript, evidenceQuotes);
    return splitIntoSegments(transcript, ranges);
  }, [transcript, evidenceQuotes, isHighlightEnabled]);

  if (!transcript) {
    return <span style={{ color: 'var(--text-muted)', fontStyle: 'italic' }}>Chưa có nội dung bóc băng.</span>;
  }

  return (
    <div
      style={{
        color: 'var(--text-main)',
        lineHeight: 1.6,
        fontSize: '0.875rem',
        whiteSpace: 'pre-wrap',
        wordBreak: 'break-word',
      }}
    >
      {segments.map((seg, idx) => {
        if (!seg.isHighlight) {
          return <span key={idx}>{seg.text}</span>;
        }

        const isCurrentActive =
          activeQuoteIndex !== null && seg.quoteIndices?.includes(activeQuoteIndex);

        return (
          <mark
            key={idx}
            id={`quote-highlight-${seg.quoteIndices?.[0] || 0}`}
            style={{
              backgroundColor: isCurrentActive
                ? 'rgba(251, 191, 36, 0.95)'
                : 'rgba(254, 240, 138, 0.7)',
              color: 'var(--slate-900)',
              padding: '0.1rem 0.3rem',
              borderRadius: '3px',
              borderBottom: isCurrentActive ? '2px solid #b45309' : '2px solid #eab308',
              fontWeight: 500,
              boxShadow: isCurrentActive ? '0 0 0 2px rgba(245, 158, 11, 0.4)' : 'none',
              transition: 'background-color 0.2s ease, box-shadow 0.2s ease',
              cursor: 'default',
            }}
            title={
              seg.quoteIndices && seg.quoteIndices.length > 0
                ? `Bằng chứng AI: Trích dẫn #${seg.quoteIndices.map((i) => i + 1).join(', #')}`
                : 'Bằng chứng AI trích dẫn'
            }
          >
            {seg.text}
          </mark>
        );
      })}
    </div>
  );
}
