import { Routes } from '@angular/router';
import { TaskList } from './tasks/task-list';
import { ResourceList } from './resources/resource-list';
import { MovementList } from './stock/movement-list';
import { FleetLive } from './fleet/fleet-live';
import { AlarmList } from './alarms/alarm-list';
import { Login } from './auth/login';
import { girisGerekli } from './auth/auth.guard';

export const routes: Routes = [
  { path: 'giris', component: Login },
  { path: '', component: TaskList, canActivate: [girisGerekli] },
  { path: 'kaynaklar', component: ResourceList, canActivate: [girisGerekli] },
  { path: 'stok', component: MovementList, canActivate: [girisGerekli] },
  { path: 'filo', component: FleetLive, canActivate: [girisGerekli] },
  { path: 'alarmlar', component: AlarmList, canActivate: [girisGerekli] },
  { path: '**', redirectTo: '' },
];
