import { Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AlarmService } from './alarm.service';
import { Alarm, AlarmGrubu } from './alarm.model';

@Component({
  selector: 'app-alarm-list',
  imports: [DatePipe],
  templateUrl: './alarm-list.html',
  styleUrl: './alarm-list.css',
})
export class AlarmList {
  private readonly service = inject(AlarmService);

  protected readonly alarmlar = this.service.son;

  constructor() {
    this.service.yenile();
  }

  protected yenile(): void {
    this.service.yenile();
  }

  protected ornekler(grup: AlarmGrubu): Alarm[] {
    return this.alarmlar().items.filter((a) => a.code === grup.code && a.severity === grup.severity);
  }
}
