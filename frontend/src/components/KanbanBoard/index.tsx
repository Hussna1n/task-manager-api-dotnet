import { useState } from 'react';
import { DndContext, closestCenter, DragEndEvent } from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy, useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { updateTask } from '../../store/tasksSlice';

interface Task {
  id: number; title: string; priority: 'low' | 'medium' | 'high';
  status: 'todo' | 'in_progress' | 'review' | 'done';
  assignee?: { name: string };
}

const COLUMNS = ['todo', 'in_progress', 'review', 'done'] as const;
const COLUMN_LABELS = { todo: 'To Do', in_progress: 'In Progress', review: 'Review', done: 'Done' };
const PRIORITY_COLORS = { low: 'bg-green-100 text-green-700', medium: 'bg-yellow-100 text-yellow-700', high: 'bg-red-100 text-red-700' };

function TaskCard({ task }: { task: Task }) {
  const { attributes, listeners, setNodeRef, transform, transition } = useSortable({ id: task.id });
  const style = { transform: CSS.Transform.toString(transform), transition };
  return (
    <div ref={setNodeRef} style={style} {...attributes} {...listeners}
      className="bg-white rounded-lg p-3 shadow-sm border border-gray-100 cursor-grab hover:shadow-md transition-shadow">
      <p className="text-sm font-medium text-gray-800">{task.title}</p>
      <div className="flex justify-between items-center mt-2">
        <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${PRIORITY_COLORS[task.priority]}`}>
          {task.priority}
        </span>
        {task.assignee && (
          <span className="text-xs text-gray-500">{task.assignee.name}</span>
        )}
      </div>
    </div>
  );
}

export default function KanbanBoard({ projectId }: { projectId: number }) {
  const dispatch = useAppDispatch();
  const tasks = useAppSelector(s => s.tasks.items.filter(t => t.projectId === projectId)) as Task[];

  async function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    if (!over || active.id === over.id) return;
    const newStatus = over.id as Task['status'];
    if (COLUMNS.includes(newStatus)) {
      dispatch(updateTask({ id: Number(active.id), status: newStatus }));
    }
  }

  return (
    <DndContext collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
      <div className="grid grid-cols-4 gap-4 p-4">
        {COLUMNS.map(col => {
          const colTasks = tasks.filter(t => t.status === col);
          return (
            <div key={col} className="bg-gray-50 rounded-xl p-3 min-h-[400px]">
              <div className="flex justify-between items-center mb-3">
                <h3 className="font-semibold text-gray-700 text-sm">{COLUMN_LABELS[col]}</h3>
                <span className="bg-gray-200 text-gray-600 text-xs rounded-full px-2 py-0.5">{colTasks.length}</span>
              </div>
              <SortableContext items={colTasks.map(t => t.id)} strategy={verticalListSortingStrategy}>
                <div className="space-y-2">
                  {colTasks.map(task => <TaskCard key={task.id} task={task} />)}
                </div>
              </SortableContext>
            </div>
          );
        })}
      </div>
    </DndContext>
  );
}
