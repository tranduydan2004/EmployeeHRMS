using System;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Models;
using FluentAssertions;
using Xunit;

namespace EmployeeHRMS.Tests.Models
{
    public class InterviewInviteTests
    {
        private static Interview CreateInvited(TimeSpan? validFor = null)
        {
            var interview = new Interview { ApplicationId = 1 };
            interview.Invite(validFor ?? TimeSpan.FromDays(3));
            return interview;
        }

        [Fact]
        public void Invite_NewInterview_ShouldSetScheduledAndExpiry()
        {
            var interview = CreateInvited(TimeSpan.FromDays(3));

            interview.Status.Should().Be(InterviewStatus.Scheduled);
            interview.ScheduledDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            interview.InviteExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(3), TimeSpan.FromSeconds(2));
            interview.ConsentedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Invite_NonPositiveDuration_ShouldThrow(int minutes)
        {
            var interview = new Interview();

            var act = () => interview.Invite(TimeSpan.FromMinutes(minutes));

            act.Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void Invite_CompletedInterview_ShouldThrow()
        {
            var interview = CreateInvited();
            interview.Complete();

            var act = () => interview.Invite(TimeSpan.FromDays(1));

            act.Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void MarkInProgress_FromScheduled_ShouldSetConsent()
        {
            var interview = CreateInvited();

            interview.MarkInProgress();

            interview.Status.Should().Be(InterviewStatus.InProgress);
            interview.ConsentedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void MarkInProgress_CalledTwice_ShouldBeIdempotentAndKeepFirstConsent()
        {
            var interview = CreateInvited();
            interview.MarkInProgress();
            var firstConsent = interview.ConsentedAt;

            interview.MarkInProgress();

            interview.Status.Should().Be(InterviewStatus.InProgress);
            interview.ConsentedAt.Should().Be(firstConsent);
        }

        [Theory]
        [InlineData(InterviewStatus.Completed)]
        [InlineData(InterviewStatus.Cancelled)]
        [InlineData(InterviewStatus.Expired)]
        public void MarkInProgress_FromTerminalState_ShouldThrow(InterviewStatus terminal)
        {
            var interview = CreateInvited();
            switch (terminal)
            {
                case InterviewStatus.Completed: interview.Complete(); break;
                case InterviewStatus.Cancelled: interview.Cancel(); break;
                case InterviewStatus.Expired: interview.MarkExpired(); break;
            }

            var act = () => interview.MarkInProgress();

            act.Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void MarkInProgress_AfterInviteExpired_ShouldThrow()
        {
            var interview = CreateInvited(TimeSpan.FromMilliseconds(1));
            System.Threading.Thread.Sleep(10);

            var act = () => interview.MarkInProgress();

            act.Should().Throw<BusinessRuleException>();
            interview.Status.Should().Be(InterviewStatus.Scheduled);
        }

        [Fact]
        public void MarkExpired_FromInProgress_ShouldSucceedAndBeIdempotent()
        {
            var interview = CreateInvited();
            interview.MarkInProgress();

            interview.MarkExpired();
            interview.MarkExpired();

            interview.Status.Should().Be(InterviewStatus.Expired);
        }

        [Fact]
        public void MarkExpired_FromCompleted_ShouldThrow()
        {
            var interview = CreateInvited();
            interview.Complete();

            var act = () => interview.MarkExpired();

            act.Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void IsInviteExpired_ShouldCompareAgainstGivenTime()
        {
            var interview = CreateInvited(TimeSpan.FromHours(1));
            var expiresAt = interview.InviteExpiresAt!.Value;

            interview.IsInviteExpired(expiresAt.AddSeconds(-1)).Should().BeFalse();
            interview.IsInviteExpired(expiresAt).Should().BeTrue();
        }

        [Fact]
        public void IsInviteExpired_WithoutInvite_ShouldBeFalse()
        {
            var interview = new Interview();
            interview.Schedule();

            interview.IsInviteExpired(DateTime.UtcNow.AddYears(10)).Should().BeFalse();
        }
    }
}
