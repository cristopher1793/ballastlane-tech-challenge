import React, { useState, useRef, useEffect, useCallback } from 'react';
import { X, Plus } from 'lucide-react';
import { cn } from '@/lib/utils';

interface LabelComboboxProps {
  value: string[];
  onChange: (labels: string[]) => void;
  availableLabels: string[];
}

const LABEL_COLORS = [
  'bg-blue-100 text-blue-800',
  'bg-purple-100 text-purple-800',
  'bg-pink-100 text-pink-800',
  'bg-orange-100 text-orange-800',
  'bg-teal-100 text-teal-800',
  'bg-indigo-100 text-indigo-800',
];

function labelColor(label: string): string {
  let hash = 0;
  for (let i = 0; i < label.length; i++) hash = label.charCodeAt(i) + ((hash << 5) - hash);
  return LABEL_COLORS[Math.abs(hash) % LABEL_COLORS.length];
}

export function LabelCombobox({ value, onChange, availableLabels }: LabelComboboxProps): React.ReactElement {
  const [inputValue, setInputValue] = useState('');
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent): void => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
        setInputValue('');
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const trimmed = inputValue.trim();

  const suggestions = availableLabels.filter(
    (l) => !value.includes(l) && l.toLowerCase().includes(trimmed.toLowerCase())
  );

  const canCreate = trimmed.length > 0 && !availableLabels.some(
    (l) => l.toLowerCase() === trimmed.toLowerCase()
  ) && !value.some((l) => l.toLowerCase() === trimmed.toLowerCase());

  const addLabel = useCallback((label: string): void => {
    const clean = label.trim();
    if (!clean || value.includes(clean)) return;
    onChange([...value, clean]);
    setInputValue('');
    inputRef.current?.focus();
  }, [value, onChange]);

  const removeLabel = (label: string): void => {
    onChange(value.filter((l) => l !== label));
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>): void => {
    if (e.key === 'Enter') {
      e.preventDefault();
      if (suggestions.length > 0 && !canCreate) {
        addLabel(suggestions[0]);
      } else if (trimmed) {
        addLabel(trimmed);
      }
    }
    if (e.key === 'Backspace' && inputValue === '' && value.length > 0) {
      onChange(value.slice(0, -1));
    }
    if (e.key === 'Escape') {
      setOpen(false);
      setInputValue('');
    }
  };

  return (
    <div ref={containerRef} className="relative">
      <div
        className={cn(
          'flex flex-wrap gap-1.5 min-h-9 w-full rounded-md border border-input bg-transparent px-2 py-1.5 text-sm shadow-sm cursor-text',
          open && 'ring-2 ring-ring'
        )}
        onClick={() => { setOpen(true); inputRef.current?.focus(); }}
      >
        {value.map((label) => (
          <span
            key={label}
            className={cn('inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium', labelColor(label))}
          >
            {label}
            <button
              type="button"
              onClick={(e) => { e.stopPropagation(); removeLabel(label); }}
              className="hover:opacity-70 cursor-pointer"
            >
              <X className="h-3 w-3" />
            </button>
          </span>
        ))}
        <input
          ref={inputRef}
          value={inputValue}
          onChange={(e) => { setInputValue(e.target.value); setOpen(true); }}
          onFocus={() => setOpen(true)}
          onKeyDown={handleKeyDown}
          placeholder={value.length === 0 ? 'Add labels...' : ''}
          className="flex-1 min-w-24 bg-transparent outline-none placeholder:text-muted-foreground"
        />
      </div>

      {open && (suggestions.length > 0 || canCreate) && (
        <div className="absolute z-50 mt-1 w-full rounded-md border bg-background shadow-md py-1 max-h-48 overflow-auto">
          {canCreate && (
            <button
              type="button"
              onMouseDown={(e) => e.preventDefault()}
              onClick={() => addLabel(trimmed)}
              className="flex w-full items-center gap-2 px-3 py-1.5 text-sm hover:bg-muted cursor-pointer text-primary font-medium"
            >
              <Plus className="h-3.5 w-3.5" />
              Create &ldquo;{trimmed}&rdquo;
            </button>
          )}
          {canCreate && suggestions.length > 0 && <div className="border-t my-1" />}
          {suggestions.map((label) => (
            <button
              key={label}
              type="button"
              onMouseDown={(e) => e.preventDefault()}
              onClick={() => addLabel(label)}
              className="flex w-full items-center gap-2 px-3 py-1.5 text-sm hover:bg-muted cursor-pointer"
            >
              <span className={cn('rounded-full px-2 py-0.5 text-xs font-medium', labelColor(label))}>
                {label}
              </span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

export { labelColor };
