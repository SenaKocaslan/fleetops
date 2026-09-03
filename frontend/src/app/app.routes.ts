import { Routes } from '@angular/router';
import { TaskList } from './tasks/task-list';
import { ResourceList } from './resources/resource-list';
import { MovementList } from './stock/movement-list';

export const routes: Routes = [
  { path: '', component: TaskList },
  { path: 'kaynaklar', component: ResourceList },
  { path: 'stok', component: MovementList },
  { path: '**', redirectTo: '' },
];
