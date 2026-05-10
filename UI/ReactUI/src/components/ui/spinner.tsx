import { cn } from '@/lib/utils';
import { Loader2 } from 'lucide-react';

interface SpinnerProps extends React.HTMLAttributes<HTMLDivElement> {
  size?: number;
}

export function Spinner({ size = 24, className, ...props }: SpinnerProps) {
  return (
    <div role="status" className={cn('flex items-center justify-center', className)} {...props}>
      <Loader2 className="animate-spin text-muted-foreground" size={size} />
      <span className="sr-only">Загрузка...</span>
    </div>
  );
}
