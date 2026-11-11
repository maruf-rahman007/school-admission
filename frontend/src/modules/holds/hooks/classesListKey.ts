/**
 * Same key as `classesQueryKey` in the classes module. Written out here because the classes module already imports
 * from holds (Apply), and importing back would create a module cycle (DECISIONS T-150 criteria). Keep both in step.
 */
export const classesListKey = ['classes'] as const
