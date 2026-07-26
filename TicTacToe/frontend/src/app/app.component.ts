import { Component } from '@angular/core';
import { GameBoardComponent } from './game-board/game-board.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [GameBoardComponent],
  template: `<app-game-board></app-game-board>`
})
export class AppComponent {}
