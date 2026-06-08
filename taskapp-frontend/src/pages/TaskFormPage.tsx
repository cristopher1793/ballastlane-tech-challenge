import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import axios from 'axios';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Spinner } from '@/components/ui/spinner';
import { taskService } from '@/services/api';
import type { TaskStatus } from '@/types';
import type { NotificationSeverity } from '@/hooks/useNotification';

interface TaskFormPageProps {
  showNotification: (message: string, severity: NotificationSeverity) => void;
}

export function TaskFormPage({ showNotification }: TaskFormPageProps): React.ReactElement {
  const { id } = useParams<{ id?: string }>();
  const isEdit = id !== undefined;
  const navigate = useNavigate();

  const [title, setTitle] = useState<string>('');
  const [description, setDescription] = useState<string>('');
  const [status, setStatus] = useState<TaskStatus>('Pending');
  const [dueDate, setDueDate] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(isEdit);
  const [saving, setSaving] = useState<boolean>(false);
  const [error, setError] = useState<string>('');

  useEffect(() => {
    if (!isEdit) return;
    const load = async (): Promise<void> => {
      try {
        const task = await taskService.getById(id!);
        setTitle(task.title);
        setDescription(task.description);
        setStatus(task.status);
        setDueDate(task.dueDate.split('T')[0]);
      } catch {
        setError('Failed to load task.');
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [id, isEdit]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>): Promise<void> => {
    e.preventDefault();
    setError('');
    setSaving(true);
    try {
      if (isEdit) {
        await taskService.update(id!, { title, description, status, dueDate: new Date(dueDate).toISOString() });
        showNotification('Task updated successfully.', 'success');
      } else {
        await taskService.create({ title, description, dueDate: new Date(dueDate).toISOString() });
        showNotification('Task created successfully.', 'success');
      }
      navigate('/tasks');
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

  if (loading) {
    return <div className="flex justify-center py-16"><Spinner size="lg" /></div>;
  }

  return (
    <div className="mx-auto max-w-lg px-4 py-8">
      <h1 className="text-2xl font-bold mb-6">{isEdit ? 'Edit Task' : 'New Task'}</h1>
      {error && (
        <Alert variant="destructive" className="mb-4">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="space-y-1.5">
          <Label htmlFor="title">Title</Label>
          <Input id="title" value={title} onChange={(e) => setTitle(e.target.value)} required maxLength={200} />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="description">Description</Label>
          <textarea
            id="description"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={3}
            className="flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring resize-none"
          />
        </div>
        {isEdit && (
          <div className="space-y-1.5">
            <Label>Status</Label>
            <Select value={status} onValueChange={(v) => setStatus(v as TaskStatus)}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Pending">Pending</SelectItem>
                <SelectItem value="InProgress">In Progress</SelectItem>
                <SelectItem value="Completed">Completed</SelectItem>
              </SelectContent>
            </Select>
          </div>
        )}
        <div className="space-y-1.5">
          <Label htmlFor="dueDate">Due Date</Label>
          <Input id="dueDate" type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} required />
        </div>
        <div className="flex gap-3 pt-2">
          <Button type="submit" disabled={saving}>
            {saving ? <Spinner size="sm" /> : isEdit ? 'Save Changes' : 'Create Task'}
          </Button>
          <Button type="button" variant="outline" onClick={() => navigate('/tasks')} disabled={saving}>
            Cancel
          </Button>
        </div>
      </form>
    </div>
  );
}
