import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Spinner } from '@/components/ui/spinner';
import { LabelCombobox } from '@/components/LabelCombobox';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import { taskService } from '@/services/api';
import { cn } from '@/lib/utils';
import type { TaskStatus } from '@/types';
import type { NotificationSeverity } from '@/hooks/useNotification';

const FIBONACCI = [1, 2, 3, 5, 8] as const;

interface TaskFormDialogProps {
  open: boolean;
  taskId: string | null;
  availableLabels: string[];
  onClose: () => void;
  onSaved: () => void;
  showNotification: (message: string, severity: NotificationSeverity) => void;
}

export function TaskFormDialog({
  open,
  taskId,
  availableLabels,
  onClose,
  onSaved,
  showNotification,
}: TaskFormDialogProps): React.ReactElement {
  const isEdit = taskId !== null;

  const [title, setTitle] = useState<string>('');
  const [description, setDescription] = useState<string>('');
  const [status, setStatus] = useState<TaskStatus>('ToDo');
  const [dueDate, setDueDate] = useState<string>('');
  const [labels, setLabels] = useState<string[]>([]);
  const [storyPoints, setStoryPoints] = useState<number | null>(null);
  const [loading, setLoading] = useState<boolean>(false);
  const [saving, setSaving] = useState<boolean>(false);
  const [error, setError] = useState<string>('');

  useEffect(() => {
    if (!open) return;
    setError('');
    if (!isEdit) {
      setTitle('');
      setDescription('');
      setStatus('ToDo');
      setDueDate('');
      setLabels([]);
      setStoryPoints(null);
      return;
    }
    const load = async (): Promise<void> => {
      setLoading(true);
      try {
        const task = await taskService.getById(taskId!);
        setTitle(task.title);
        setDescription(task.description);
        setStatus(task.status);
        setDueDate(task.dueDate.split('T')[0]);
        setLabels(task.labels ?? []);
        setStoryPoints(task.storyPoints ?? null);
      } catch {
        setError('Failed to load task.');
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [open, taskId, isEdit]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>): Promise<void> => {
    e.preventDefault();
    setError('');
    setSaving(true);
    try {
      if (isEdit) {
        await taskService.update(taskId!, {
          title, description, status,
          dueDate: new Date(dueDate).toISOString(),
          labels, storyPoints,
        });
        showNotification('Task updated successfully.', 'success');
      } else {
        await taskService.create({
          title, description,
          dueDate: new Date(dueDate).toISOString(),
          labels, storyPoints,
        });
        showNotification('Task created successfully.', 'success');
      }
      onSaved();
    } catch (err: unknown) {
      if (axios.isAxiosError(err) && err.response) {
        const data = err.response.data as { error?: string };
        setError(data?.error ?? 'Save failed.');
      } else {
        setError('An unexpected error occurred.');
      }
    } finally {
      setSaving(false);
    }
  };

  const allAvailable = Array.from(new Set([...availableLabels, ...labels]));

  const toggleSP = (sp: number): void => {
    setStoryPoints(storyPoints === sp ? null : sp);
  };

  return (
    <Dialog open={open} onOpenChange={(o) => { if (!o) onClose(); }}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{isEdit ? 'Edit Task' : 'New Task'}</DialogTitle>
        </DialogHeader>

        {loading ? (
          <div className="flex justify-center py-8"><Spinner size="lg" /></div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-4 pt-2">
            {error && (
              <Alert variant="destructive">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            <div className="space-y-1.5">
              <Label htmlFor="dlg-title">Title</Label>
              <Input
                id="dlg-title"
                placeholder="Task title"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                required
                maxLength={200}
              />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="dlg-description">Description</Label>
              <textarea
                id="dlg-description"
                placeholder="Optional description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={3}
                className="flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring resize-none placeholder:text-muted-foreground"
              />
            </div>

            {/* Story points picker */}
            <div className="space-y-1.5">
              <Label>
                Story Points
                <span className="ml-1.5 text-xs font-normal text-muted-foreground">(optional)</span>
              </Label>
              <div className="flex gap-2">
                {FIBONACCI.map((sp) => (
                  <button
                    key={sp}
                    type="button"
                    onClick={() => toggleSP(sp)}
                    className={cn(
                      'h-9 w-11 rounded-md border text-sm font-semibold transition-colors',
                      storyPoints === sp
                        ? 'bg-primary text-primary-foreground border-primary'
                        : 'border-input bg-background hover:bg-muted hover:border-ring'
                    )}
                  >
                    {sp}
                  </button>
                ))}
                {storyPoints !== null && (
                  <button
                    type="button"
                    onClick={() => setStoryPoints(null)}
                    className="h-9 px-3 rounded-md border border-input bg-background text-xs text-muted-foreground hover:bg-muted"
                  >
                    Clear
                  </button>
                )}
              </div>
            </div>

            <div className="space-y-1.5">
              <Label>Labels</Label>
              <LabelCombobox
                value={labels}
                onChange={setLabels}
                availableLabels={allAvailable}
              />
            </div>

            {isEdit && (
              <div className="space-y-1.5">
                <Label>Status</Label>
                <Select value={status} onValueChange={(v) => setStatus(v as TaskStatus)}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="ToDo">To Do</SelectItem>
                    <SelectItem value="Pending">Pending</SelectItem>
                    <SelectItem value="InProgress">In Progress</SelectItem>
                    <SelectItem value="Completed">Completed</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            )}

            <div className="space-y-1.5">
              <Label htmlFor="dlg-dueDate">Due Date</Label>
              <Input
                id="dlg-dueDate"
                type="date"
                value={dueDate}
                onChange={(e) => setDueDate(e.target.value)}
                required
              />
            </div>

            <DialogFooter className="pt-2">
              <Button type="button" variant="outline" onClick={onClose} disabled={saving}>
                Cancel
              </Button>
              <Button type="submit" disabled={saving}>
                {saving ? <Spinner size="sm" /> : isEdit ? 'Save Changes' : 'Create Task'}
              </Button>
            </DialogFooter>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
