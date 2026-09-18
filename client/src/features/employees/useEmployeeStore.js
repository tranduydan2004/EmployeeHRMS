import { create } from 'zustand';

// Feature-local Zustand store for state that does NOT belong in the URL
// (e.g. checked rows for bulk actions, column visibility, active drawer)
export const useEmployeeStore = create((set) => ({
  selectedEmployeeIds: [],

  toggleSelectEmployee: (id) =>
    set((state) => {
      const exists = state.selectedEmployeeIds.includes(id);
      return {
        selectedEmployeeIds: exists
          ? state.selectedEmployeeIds.filter((empId) => empId !== id)
          : [...state.selectedEmployeeIds, id],
      };
    }),

  selectAllEmployees: (ids) => set({ selectedEmployeeIds: ids }),

  clearSelection: () => set({ selectedEmployeeIds: [] }),
}));
