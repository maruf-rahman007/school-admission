import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createClass } from '../api/classesApi'
import { classesQueryKey } from './useClasses'

/** POST /classes. Never retried automatically (no duplicate classes); refreshes every class list on success. */
export function useCreateClass() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createClass,
    retry: false,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: classesQueryKey }),
  })
}
