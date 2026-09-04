import { Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AlarmService } from './alarm.service';

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
}
