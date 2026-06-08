import React, { useState, useEffect, useCallback } from 'react';
import { Plus, Pencil, Trash2, Calendar } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Spinner } from '@/components/ui/spinner';
import { DeleteConfirmDialog } from '@/components/DeleteConfirmDialog';
import { TaskFormDialog } from '@/components/TaskFormDialog';
import { Paginator } from '@/components/Paginator';
import { labelColor } from '@/components/LabelCombobox';
import { taskService } from '@/services/api';
import { cn } from '@/lib/utils';
import type { TaskResponseDto, TaskStatus, UpdateTaskDto } from '@/types';
import type { NotificationSeverity } from '@/hooks/useNotification';

interface TasksPageProps {
  showNotification: (message: string, severity: NotificationSeverity) => void;
}

type StatusFilter = 'All' | TaskStatus;

const statusLabels: Record<TaskStatus, string> = {
  ToDo: 'To Do',
  Pending: 'Pending',
  InProgress: 'In Progress',
  Completed: 'Completed',
};

const statusTriggerClasses: Record<TaskStatus, string> = {
  ToDo:       'bg-gray-100   text-gray-600   hover:bg-gray-200',
  Pending:    'bg-yellow-100 text-yellow-800  hover:bg-yellow-200',
  InProgress: 'bg-sky-100    text-sky-800    hover:bg-sky-200',
  Completed:  'bg-green-100  text-green-800   hover:bg-green-200',
};

export function TasksPage({ showNotification }: TasksPageProps): React.ReactElement {
  const [tasks, setTasks] = useState<TaskResponseDto[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('All');
  const [labelFilter, setLabelFilter] = useState<string>('All');

  const [formOpen, setFormOpen] = useState<boolean>(false);
  const [editingTaskId, setEditingTaskId] = useState<string | null>(null);

  const [deleteDialogOpen, setDeleteDialogOpen] = useState<boolean>(false);
  const [taskToDelete, setTaskToDelete] = useState<TaskResponseDto | null>(null);

  const [ownerFilter, setOwnerFilter] = useState<string>('All');
  const [savingCell, setSavingCell] = useState<string | null>(null);
  const [editingDate, setEditingDate] = useState<{ taskId: string; value: string } | null>(null);

  const [currentPage, setCurrentPage] = useState<number>(1);
  const [pageSize, setPageSize] = useState<number>(10);

  const loadTasks = useCallback(async (): Promise<void> => {
    try {
      setLoading(true);
      const data = await taskService.getAll();
      setTasks(data);
    } catch {
      showNotification('Failed to load tasks.', 'error');
    } finally {
      setLoading(false);
    }
  }, [showNotification]);

  useEffect(() => { loadTasks(); }, [loadTasks]);

  const availableLabels = Array.from(new Set(tasks.flatMap((t) => t.labels ?? []))).sort();
  const isAdminView = tasks.some((t) => t.ownerUsername != null);
  const availableOwners = isAdminView
    ? Array.from(new Set(tasks.map((t) => t.ownerUsername).filter(Boolean) as string[])).sort()
    : [];

  const handleInlineUpdate = useCallback(async (task: TaskResponseDto, patch: Partial<UpdateTaskDto>): Promise<void> => {
    setSavingCell(task.id);
    try {
      const updated = await taskService.update(task.id, {
        title: task.title,
        description: task.description,
        status: patch.status ?? task.status,
        dueDate: patch.dueDate ?? task.dueDate,
        labels: patch.labels ?? task.labels ?? [],
        storyPoints: task.storyPoints ?? null,
      });
      setTasks((prev) => prev.map((t) => (t.id === updated.id ? updated : t)));
    } catch {
      showNotification('Failed to update task.', 'error');
    } finally {
      setSavingCell(null);
      setEditingDate(null);
    }
  }, [showNotification]);

  const commitDateEdit = (task: TaskResponseDto, rawValue: string): void => {
    if (!rawValue) { setEditingDate(null); return; }
    const newIso = new Date(rawValue).toISOString();
    if (newIso !== task.dueDate) {
      handleInlineUpdate(task, { dueDate: newIso });
    } else {
      setEditingDate(null);
    }
  };

  const openCreate = (): void => { setEditingTaskId(null); setFormOpen(true); };
  const openEdit = (task: TaskResponseDto): void => { setEditingTaskId(task.id); setFormOpen(true); };
  const handleFormSaved = async (): Promise<void> => { setFormOpen(false); await loadTasks(); };
  const handleDeleteClick = (task: TaskResponseDto): void => { setTaskToDelete(task); setDeleteDialogOpen(true); };
  const handleDeleteConfirm = async (): Promise<void> => {
    if (!taskToDelete) return;
    try {
      await taskService.delete(taskToDelete.id);
      showNotification('Task deleted successfully.', 'success');
      setDeleteDialogOpen(false);
      setTaskToDelete(null);
      await loadTasks();
    } catch {
      showNotification('Failed to delete task.', 'error');
    }
  };

  const filteredTasks = tasks.filter((t) => {
    const statusMatch = statusFilter === 'All' || t.status === statusFilter;
    const labelMatch = labelFilter === 'All' || (t.labels ?? []).includes(labelFilter);
    const ownerMatch = ownerFilter === 'All' || t.ownerUsername === ownerFilter;
    return statusMatch && labelMatch && ownerMatch;
  });

  // Reset to first page whenever filters change
  useEffect(() => { setCurrentPage(1); }, [statusFilter, labelFilter, ownerFilter]);

  const paginatedTasks = filteredTasks.slice((currentPage - 1) * pageSize, currentPage * pageSize);

  // Shared inline status select (used in both table and cards)
  const InlineStatus = ({ task }: { task: TaskResponseDto }): React.ReactElement => (
    <Select
      value={task.status}
      onValueChange={(v) => handleInlineUpdate(task, { status: v as TaskStatus })}
      disabled={savingCell === task.id}
    >
      <SelectTrigger className={cn(
        'h-auto w-auto gap-1 border-0 px-2.5 py-0.5 rounded-full text-xs font-semibold shadow-none focus:ring-0 cursor-pointer transition-colors',
        statusTriggerClasses[task.status]
      )}>
        <SelectValue />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="ToDo">To Do</SelectItem>
        <SelectItem value="Pending">Pending</SelectItem>
        <SelectItem value="InProgress">In Progress</SelectItem>
        <SelectItem value="Completed">Completed</SelectItem>
      </SelectContent>
    </Select>
  );

  // Shared inline due date (used in both table and cards)
  const InlineDate = ({ task }: { task: TaskResponseDto }): React.ReactElement => (
    editingDate?.taskId === task.id ? (
      <input
        type="date"
        autoFocus
        className="rounded border border-input px-2 py-0.5 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
        value={editingDate.value}
        onChange={(e) => setEditingDate({ taskId: task.id, value: e.target.value })}
        onBlur={() => commitDateEdit(task, editingDate.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter') commitDateEdit(task, editingDate.value);
          if (e.key === 'Escape') setEditingDate(null);
        }}
      />
    ) : (
      <span
        className={cn(
          'inline-flex items-center gap-1 cursor-pointer rounded px-1 hover:bg-muted hover:text-primary transition-colors text-sm',
          savingCell === task.id && 'opacity-50 pointer-events-none'
        )}
        title="Click to edit"
        onClick={() => setEditingDate({ taskId: task.id, value: task.dueDate.split('T')[0] })}
      >
        <Calendar className="h-3.5 w-3.5 text-muted-foreground" />
        {new Date(task.dueDate).toLocaleDateString()}
      </span>
    )
  );

  return (
    <div className="mx-auto max-w-7xl px-4 py-6">
      <div className="flex items-center justify-between mb-5">
        <h1 className="text-2xl font-bold">My Tasks</h1>
        <Button onClick={openCreate}><Plus className="h-4 w-4 mr-1" /> New Task</Button>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap gap-3 mb-4">
        <Select value={statusFilter} onValueChange={(v) => setStatusFilter(v as StatusFilter)}>
          <SelectTrigger className="w-40"><SelectValue placeholder="Status" /></SelectTrigger>
          <SelectContent>
            <SelectItem value="All">All statuses</SelectItem>
            <SelectItem value="ToDo">To Do</SelectItem>
            <SelectItem value="Pending">Pending</SelectItem>
            <SelectItem value="InProgress">In Progress</SelectItem>
            <SelectItem value="Completed">Completed</SelectItem>
          </SelectContent>
        </Select>

        <Select value={labelFilter} onValueChange={setLabelFilter}>
          <SelectTrigger className="w-40"><SelectValue placeholder="Label" /></SelectTrigger>
          <SelectContent>
            <SelectItem value="All">All labels</SelectItem>
            {availableLabels.map((label) => (
              <SelectItem key={label} value={label}>
                <span className={cn('rounded-full px-2 py-0.5 text-xs font-medium', labelColor(label))}>{label}</span>
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        {isAdminView && (
          <Select value={ownerFilter} onValueChange={setOwnerFilter}>
            <SelectTrigger className="w-40"><SelectValue placeholder="Owner" /></SelectTrigger>
            <SelectContent>
              <SelectItem value="All">All users</SelectItem>
              {availableOwners.map((owner) => (
                <SelectItem key={owner} value={owner}>{owner}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}
      </div>

      {loading ? (
        <div className="flex justify-center py-12"><Spinner size="lg" /></div>
      ) : filteredTasks.length === 0 ? (
        <div className="rounded-md border py-12 text-center text-muted-foreground">No tasks found.</div>
      ) : (
        <>
          {/* ── Mobile cards (< md) ── */}
          <div className="md:hidden space-y-3">
            {paginatedTasks.map((task) => {
              const isSaving = savingCell === task.id;
              return (
                <div key={task.id} className={cn('rounded-xl border bg-background p-4 shadow-sm', isSaving && 'opacity-60')}>
                  {/* Title + actions */}
                  <div className="flex items-start justify-between gap-2">
                    <p className="font-semibold leading-snug">{task.title}</p>
                    <div className="flex items-center gap-0.5 shrink-0">
                      {isSaving ? <Spinner size="sm" /> : (
                        <>
                          <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => openEdit(task)}>
                            <Pencil className="h-3.5 w-3.5" />
                          </Button>
                          <Button variant="ghost" size="icon" className="h-8 w-8 text-destructive hover:text-destructive" onClick={() => handleDeleteClick(task)}>
                            <Trash2 className="h-3.5 w-3.5" />
                          </Button>
                        </>
                      )}
                    </div>
                  </div>

                  {/* Description */}
                  {task.description && (
                    <p className="text-sm text-muted-foreground mt-1 line-clamp-2">{task.description}</p>
                  )}

                  {/* Labels */}
                  {(task.labels ?? []).length > 0 && (
                    <div className="flex flex-wrap gap-1 mt-2">
                      {(task.labels ?? []).map((label) => (
                        <span key={label} className={cn('rounded-full px-2 py-0.5 text-xs font-medium', labelColor(label))}>
                          {label}
                        </span>
                      ))}
                    </div>
                  )}

                  {/* Owner (admin view only) */}
                  {task.ownerUsername && (
                    <p className="text-xs text-muted-foreground mt-1.5">
                      <span className="font-medium">Owner:</span> {task.ownerUsername}
                    </p>
                  )}

                  {/* Status + SP + Due date */}
                  <div className="flex flex-wrap items-center gap-3 mt-3 pt-3 border-t">
                    <InlineStatus task={task} />
                    {task.storyPoints != null && (
                      <span className="inline-flex items-center justify-center h-6 w-7 rounded bg-slate-100 text-slate-600 text-xs font-bold">
                        {task.storyPoints}
                      </span>
                    )}
                    <InlineDate task={task} />
                  </div>
                </div>
              );
            })}
          </div>

          {/* ── Desktop table (≥ md) ── */}
          <div className="hidden md:block rounded-md border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Title</TableHead>
                  <TableHead>Description</TableHead>
                  <TableHead>Labels</TableHead>
                  {isAdminView && <TableHead>Owner</TableHead>}
                  <TableHead className="w-12 text-center">SP</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Due Date</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedTasks.map((task) => {
                  const isSaving = savingCell === task.id;
                  return (
                    <TableRow key={task.id}>
                      <TableCell className="font-medium">{task.title}</TableCell>
                      <TableCell className="text-muted-foreground">{task.description}</TableCell>
                      <TableCell>
                        <div className="flex flex-wrap gap-1">
                          {(task.labels ?? []).map((label) => (
                            <span key={label} className={cn('rounded-full px-2 py-0.5 text-xs font-medium', labelColor(label))}>
                              {label}
                            </span>
                          ))}
                        </div>
                      </TableCell>
                      {isAdminView && (
                        <TableCell className="text-sm text-muted-foreground">
                          {task.ownerUsername}
                        </TableCell>
                      )}
                      <TableCell className="text-center">
                        {task.storyPoints != null && (
                          <span className="inline-flex items-center justify-center h-6 w-7 rounded bg-slate-100 text-slate-600 text-xs font-bold">
                            {task.storyPoints}
                          </span>
                        )}
                      </TableCell>
                      <TableCell>
                        {isSaving && editingDate === null ? <Spinner size="sm" /> : <InlineStatus task={task} />}
                      </TableCell>
                      <TableCell>
                        <InlineDate task={task} />
                      </TableCell>
                      <TableCell className="text-right">
                        <Button variant="ghost" size="icon" onClick={() => openEdit(task)} disabled={isSaving}>
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button variant="ghost" size="icon" className="text-destructive hover:text-destructive" onClick={() => handleDeleteClick(task)} disabled={isSaving}>
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </div>

          <Paginator
            currentPage={currentPage}
            totalItems={filteredTasks.length}
            pageSize={pageSize}
            onPageChange={setCurrentPage}
            onPageSizeChange={(size) => { setPageSize(size); setCurrentPage(1); }}
          />
        </>
      )}

      <TaskFormDialog
        open={formOpen}
        taskId={editingTaskId}
        availableLabels={availableLabels}
        onClose={() => setFormOpen(false)}
        onSaved={handleFormSaved}
        showNotification={showNotification}
      />

      <DeleteConfirmDialog
        open={deleteDialogOpen}
        taskTitle={taskToDelete?.title ?? ''}
        onConfirm={handleDeleteConfirm}
        onCancel={() => { setDeleteDialogOpen(false); setTaskToDelete(null); }}
      />
    </div>
  );
}
