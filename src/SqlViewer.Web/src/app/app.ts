import { Component } from '@angular/core';
import { Toolbar } from './components/toolbar/toolbar';
import { RequestList } from './components/request-list/request-list';

@Component({
  selector: 'app-root',
  imports: [Toolbar, RequestList],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {}
