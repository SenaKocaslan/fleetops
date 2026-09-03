import { Routes } from '@angular/router';
import { TaskList } from './tasks/task-list';
import { ResourceList } from './resources/resource-list';

export const routes: Routes = [
  { path: '', component: TaskList },
  { path: 'kaynaklar', component: ResourceList },
  { path: '**', redirectTo: '' },
];
