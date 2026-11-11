/** The app's mark: the first letter of "ভর্তি" (admission) on school-board green. Same in both languages. */
export function BrandMark({ className = 'size-8 text-lg' }: { className?: string }) {
  return (
    <span
      aria-hidden="true"
      className={`grid shrink-0 place-items-center rounded-lg bg-primary font-semibold leading-none text-primary-foreground ${className}`}
    >
      ভ
    </span>
  )
}
