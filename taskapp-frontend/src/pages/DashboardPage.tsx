import React, { useState, useEffect } from 'react';
import {
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Legend,
  Tooltip,
  ComposedChart,
  BarChart,
  Bar,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  ReferenceLine,
} from 'recharts';
import { Spinner } from '@/components/ui/spinner';
import { taskService } from '@/services/api';
import { cn } from '@/lib/utils';
import type { DashboardStatsDto, CompletionTimingEntry, WeeklyVelocityEntry, EstimationAccuracyEntry } from '@/types';
import type { NotificationSeverity } from '@/hooks/useNotification';

const DAY_MS = 86_400_000;

interface DashboardPageProps {
  showNotification: (message: string, severity: NotificationSeverity) => void;
}

const STATUS_COLORS: Record<string, string> = {
  'To Do': '#9ca3af',
  Pending: '#fbbf24',
  'In Progress': '#38bdf8',
  Completed: '#4ade80',
};

interface StatCardProps {
  label: string;
  count: number;
  accent: string;
  bg: string;
}

function StatCard({ label, count, accent, bg }: StatCardProps): React.ReactElement {
  return (
    <div className={cn('rounded-xl border p-4 shadow-sm', bg)}>
      <p className="text-sm text-muted-foreground">{label}</p>
      <p className={cn('text-3xl font-bold mt-1', accent)}>{count}</p>
    </div>
  );
}

interface DumbEntry {
  name: string;
  fullTitle: string;
  dueDate: string;
  completedAt: string;
  dueTs: number;
  completedTs: number;
  range: [number, number];
  isEarly: boolean;
  daysDiff: number;
}

interface TimingTooltipProps {
  active?: boolean;
  payload?: Array<{ payload: DumbEntry }>;
}

function TimingTooltip({ active, payload }: TimingTooltipProps): React.ReactElement | null {
  if (!active || !payload?.length) return null;
  const d = payload[0].payload;
  return (
    <div className="rounded border bg-background px-3 py-2 text-sm shadow-lg min-w-[210px]">
      <p className="font-semibold mb-2 max-w-[210px] line-clamp-2">{d.fullTitle}</p>
      <div className="flex items-center gap-2 text-muted-foreground">
        <span className="inline-block h-2.5 w-2.5 rounded-sm bg-blue-400 flex-shrink-0" />
        Due: {new Date(d.dueDate).toLocaleDateString()}
      </div>
      <div className="flex items-center gap-2 text-muted-foreground mt-1">
        <span className={cn(
          'inline-block h-2.5 w-2.5 rounded-full flex-shrink-0',
          d.isEarly ? 'bg-green-400' : 'bg-red-400'
        )} />
        Completed: {new Date(d.completedAt).toLocaleDateString()}
      </div>
      <p className={cn('font-medium mt-2', d.isEarly ? 'text-green-600' : 'text-red-600')}>
        {d.isEarly
          ? `${d.daysDiff} day${d.daysDiff !== 1 ? 's' : ''} early`
          : `${d.daysDiff} day${d.daysDiff !== 1 ? 's' : ''} late`}
      </p>
    </div>
  );
}

export function DashboardPage({ showNotification }: DashboardPageProps): React.ReactElement {
  const [stats, setStats] = useState<DashboardStatsDto | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    (async () => {
      try {
        const data = await taskService.getDashboardStats();
        setStats(data);
      } catch {
        showNotification('Failed to load dashboard.', 'error');
      } finally {
        setLoading(false);
      }
    })();
  }, [showNotification]);

  if (loading) {
    return (
      <div className="flex justify-center py-24">
        <Spinner size="lg" />
      </div>
    );
  }

  if (!stats) {
    return (
      <div className="mx-auto max-w-7xl px-4 py-6 text-center text-muted-foreground">
        Could not load dashboard data.
      </div>
    );
  }

  const pieData = [
    { name: 'To Do', value: stats.toDo },
    { name: 'Pending', value: stats.pending },
    { name: 'In Progress', value: stats.inProgress },
    { name: 'Completed', value: stats.completed },
  ].filter((d) => d.value > 0);

  // Build dumbbell entries — one row per completed task
  const dumbData: DumbEntry[] = (stats.timings as CompletionTimingEntry[]).map((t) => {
    const due = new Date(t.dueDate).getTime();
    const comp = new Date(t.completedAt).getTime();
    const isEarly = comp <= due;
    const daysDiff = Math.abs(Math.round((due - comp) / DAY_MS));
    return {
      name: t.title.length > 18 ? `${t.title.slice(0, 17)}…` : t.title,
      fullTitle: t.title,
      dueDate: t.dueDate,
      completedAt: t.completedAt,
      dueTs: due,
      completedTs: comp,
      range: [Math.min(due, comp), Math.max(due, comp)],
      isEarly,
      daysDiff,
    };
  });

  const allTs = dumbData.flatMap((d) => [d.dueTs, d.completedTs]);
  const minTs = allTs.length ? Math.min(...allTs) - DAY_MS * 3 : 0;
  const maxTs = allTs.length ? Math.max(...allTs) + DAY_MS * 3 : 0;
  const chartHeight = Math.max(200, dumbData.length * 52 + 60);

  const formatTick = (ms: number) =>
    new Date(ms).toLocaleDateString('en', { month: 'short', day: 'numeric' });

  return (
    <div className="mx-auto max-w-7xl px-4 py-6">
      <h1 className="text-2xl font-bold mb-6">Dashboard</h1>

      {/* Stat cards */}
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-4 mb-8">
        <StatCard label="Total Tasks"  count={stats.totalTasks}  accent="text-foreground"   bg="bg-background"  />
        <StatCard label="To Do"        count={stats.toDo}        accent="text-gray-500"     bg="bg-gray-50"     />
        <StatCard label="Pending"      count={stats.pending}     accent="text-yellow-600"   bg="bg-yellow-50"   />
        <StatCard label="In Progress"  count={stats.inProgress}  accent="text-sky-600"      bg="bg-sky-50"      />
        <StatCard label="Completed"    count={stats.completed}   accent="text-green-600"    bg="bg-green-50"    />
      </div>

      {stats.totalTasks === 0 ? (
        <div className="rounded-xl border bg-background py-16 text-center text-muted-foreground shadow-sm">
          No tasks yet. Create your first task to see stats here.
        </div>
      ) : (
        <>
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">

          {/* Status distribution */}
          <div className="rounded-xl border bg-background shadow-sm p-5">
            <h2 className="text-base font-semibold mb-4">Status Distribution</h2>
            {pieData.length === 0 ? (
              <div className="flex h-[280px] items-center justify-center text-sm text-muted-foreground">No data</div>
            ) : (
              <ResponsiveContainer width="100%" height={280}>
                <PieChart>
                  <Pie
                    data={pieData}
                    dataKey="value"
                    nameKey="name"
                    cx="50%"
                    cy="50%"
                    outerRadius={100}
                    label={({ percent }) => percent > 0.05 ? `${(percent * 100).toFixed(0)}%` : ''}
                    labelLine={false}
                  >
                    {pieData.map((entry) => (
                      <Cell key={entry.name} fill={STATUS_COLORS[entry.name]} />
                    ))}
                  </Pie>
                  <Tooltip formatter={(value: number) => [value, 'Tasks']} />
                  <Legend />
                </PieChart>
              </ResponsiveContainer>
            )}
          </div>

          {/* Due date vs Completed — dumbbell chart */}
          <div className="rounded-xl border bg-background shadow-sm p-5">
            <h2 className="text-base font-semibold mb-1">Due Date vs Completed</h2>
            <p className="text-xs text-muted-foreground mb-3">
              Expected deadline compared to actual completion
            </p>

            {dumbData.length === 0 ? (
              <div className="flex h-[280px] items-center justify-center text-sm text-muted-foreground">
                No completed tasks with timing data yet.
              </div>
            ) : (
              <>
                {/* Summary + legend */}
                <div className="flex flex-wrap items-center gap-x-6 gap-y-2 mb-4">
                  <span className="text-sm">
                    <span className="text-muted-foreground">On-time </span>
                    <span className="font-semibold">{stats.onTimeRate.toFixed(1)}%</span>
                  </span>
                  <span className="text-sm">
                    <span className="text-muted-foreground">Avg </span>
                    <span className={cn('font-semibold', stats.averageDaysVariance >= 0 ? 'text-green-600' : 'text-red-600')}>
                      {stats.averageDaysVariance >= 0 ? '+' : ''}{stats.averageDaysVariance.toFixed(1)} days
                    </span>
                  </span>
                  <span className="flex items-center gap-4 text-xs text-muted-foreground ml-auto">
                    <span className="flex items-center gap-1.5">
                      <span className="inline-block h-2.5 w-2.5 rounded-sm bg-blue-400" />
                      Due date
                    </span>
                    <span className="flex items-center gap-1.5">
                      <span className="inline-block h-2.5 w-2.5 rounded-full bg-green-400" />
                      Early
                    </span>
                    <span className="flex items-center gap-1.5">
                      <span className="inline-block h-2.5 w-2.5 rounded-full bg-red-400" />
                      Late
                    </span>
                  </span>
                </div>

                <div style={{ height: chartHeight }}>
                  <ResponsiveContainer width="100%" height="100%">
                    <ComposedChart
                      layout="vertical"
                      data={dumbData}
                      margin={{ top: 5, right: 10, bottom: 5, left: 0 }}
                    >
                      <CartesianGrid strokeDasharray="3 3" vertical horizontal={false} />
                      <XAxis
                        type="number"
                        domain={[minTs, maxTs]}
                        tickFormatter={formatTick}
                        tick={{ fontSize: 10 }}
                        tickCount={5}
                      />
                      <YAxis
                        type="category"
                        dataKey="name"
                        width={110}
                        tick={{ fontSize: 11 }}
                      />
                      <Tooltip content={<TimingTooltip />} />

                      {/* Thin connector bar from min to max of the two dates */}
                      <Bar dataKey="range" barSize={4} legendType="none">
                        {dumbData.map((entry, i) => (
                          <Cell key={i} fill={entry.isEarly ? '#bbf7d0' : '#fecaca'} />
                        ))}
                      </Bar>

                      {/* Due date — blue square dot */}
                      <Line
                        dataKey="dueTs"
                        strokeWidth={0}
                        dot={{ r: 7, fill: '#60a5fa', stroke: '#fff', strokeWidth: 2 }}
                        activeDot={{ r: 9, fill: '#60a5fa' }}
                        legendType="none"
                      />

                      {/* Completed date — green (early) or red (late) circle */}
                      <Line
                        dataKey="completedTs"
                        strokeWidth={0}
                        dot={(props: React.SVGProps<SVGCircleElement> & { index?: number; cx?: number; cy?: number }) => {
                          const entry = dumbData[props.index ?? 0];
                          return (
                            <circle
                              key={`comp-${props.index}`}
                              cx={props.cx}
                              cy={props.cy}
                              r={7}
                              fill={entry?.isEarly ? '#4ade80' : '#f87171'}
                              stroke="#fff"
                              strokeWidth={2}
                            />
                          );
                        }}
                        activeDot={(props: React.SVGProps<SVGCircleElement> & { index?: number; cx?: number; cy?: number }) => {
                          const entry = dumbData[props.index ?? 0];
                          return (
                            <circle
                              key={`comp-active-${props.index}`}
                              cx={props.cx}
                              cy={props.cy}
                              r={9}
                              fill={entry?.isEarly ? '#4ade80' : '#f87171'}
                              stroke="#fff"
                              strokeWidth={2}
                            />
                          );
                        }}
                        legendType="none"
                      />
                    </ComposedChart>
                  </ResponsiveContainer>
                </div>
              </>
            )}
          </div>
        </div>

        {/* Velocity + Accuracy row */}

        {(stats.weeklyVelocity.length > 0 || stats.estimationAccuracy.length > 0) && (
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mt-6">

            {/* Weekly velocity */}
            {stats.weeklyVelocity.length > 0 && (
              <div className="rounded-xl border bg-background shadow-sm p-5">
                <h2 className="text-base font-semibold mb-1">Weekly Velocity</h2>
                <p className="text-xs text-muted-foreground mb-4">
                  Story points completed per week
                </p>
                <ResponsiveContainer width="100%" height={220}>
                  <BarChart
                    data={stats.weeklyVelocity as WeeklyVelocityEntry[]}
                    margin={{ top: 5, right: 10, bottom: 20, left: 0 }}
                  >
                    <CartesianGrid strokeDasharray="3 3" vertical={false} />
                    <XAxis
                      dataKey="week"
                      tick={{ fontSize: 11 }}
                      angle={-30}
                      textAnchor="end"
                    />
                    <YAxis tick={{ fontSize: 12 }} allowDecimals={false} />
                    <Tooltip
                      formatter={(value: number, name: string) => [
                        value,
                        name === 'points' ? 'Story Points' : 'Tasks',
                      ]}
                    />
                    <Bar dataKey="points" fill="#60a5fa" radius={[4, 4, 0, 0]} maxBarSize={48} name="points" />
                    <Bar dataKey="tasks" fill="#cbd5e1" radius={[4, 4, 0, 0]} maxBarSize={48} name="tasks" />
                  </BarChart>
                </ResponsiveContainer>
                <div className="flex gap-4 text-xs text-muted-foreground mt-1">
                  <span className="flex items-center gap-1.5">
                    <span className="inline-block h-2.5 w-3 rounded-sm bg-blue-400" />
                    Story points
                  </span>
                  <span className="flex items-center gap-1.5">
                    <span className="inline-block h-2.5 w-3 rounded-sm bg-slate-300" />
                    Task count
                  </span>
                </div>
              </div>
            )}

            {/* Estimation accuracy */}
            {stats.estimationAccuracy.length > 0 && (
              <div className="rounded-xl border bg-background shadow-sm p-5">
                <h2 className="text-base font-semibold mb-1">Estimation Accuracy</h2>
                <p className="text-xs text-muted-foreground mb-4">
                  Average days to complete tasks, by story point size
                </p>
                <ResponsiveContainer width="100%" height={220}>
                  <BarChart
                    data={stats.estimationAccuracy as EstimationAccuracyEntry[]}
                    margin={{ top: 5, right: 10, bottom: 5, left: 0 }}
                  >
                    <CartesianGrid strokeDasharray="3 3" vertical={false} />
                    <XAxis
                      dataKey="storyPoints"
                      tick={{ fontSize: 12 }}
                      label={{ value: 'SP', position: 'insideBottom', offset: -2, fontSize: 11 }}
                    />
                    <YAxis tick={{ fontSize: 12 }} unit=" d" />
                    <ReferenceLine
                      y={1}
                      stroke="#94a3b8"
                      strokeDasharray="4 2"
                      label={{ value: '1 day / SP', position: 'right', fontSize: 10, fill: '#94a3b8' }}
                    />
                    <Tooltip
                      formatter={(value: number, _: string, props: { payload?: EstimationAccuracyEntry }) => [
                        `${value} days (${props.payload?.count ?? 0} task${(props.payload?.count ?? 0) !== 1 ? 's' : ''})`,
                        'Avg days',
                      ]}
                      labelFormatter={(label) => `${label} story point${label !== 1 ? 's' : ''}`}
                    />
                    <Bar dataKey="avgDays" fill="#a78bfa" radius={[4, 4, 0, 0]} maxBarSize={56} name="avgDays" />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            )}
          </div>
        )}
        </>
      )}
    </div>
  );
}
