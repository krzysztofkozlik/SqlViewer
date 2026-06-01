import { Component, inject } from '@angular/core';
import { MatExpansionModule } from '@angular/material/expansion';
import { MonitoringService } from '../../services/monitoring.service';
import { RequestRow } from '../request-row/request-row';

@Component({
  selector: 'app-request-list',
  imports: [MatExpansionModule, RequestRow],
  templateUrl: './request-list.html',
  styleUrl: './request-list.scss',
})
export class RequestList {
  protected readonly monitoring = inject(MonitoringService);
}
