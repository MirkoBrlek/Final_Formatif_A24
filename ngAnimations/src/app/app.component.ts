import {
  trigger,
  transition,
  useAnimation,
  AnimationEvent,
} from '@angular/animations';
import { Component } from '@angular/core';
import { bounce, shakeX, tada } from 'ng-animate';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
  animations: [
    trigger('shakeRed', [
      transition('* => active', useAnimation(shakeX, { params: { timing: 2 } })),
    ]),
    trigger('bounceGreen', [
      transition('* => active', useAnimation(bounce, { params: { timing: 4 } })),
    ]),
    trigger('tadaBlue', [
      transition('* => active', useAnimation(tada, { params: { timing: 3 } })),
    ]),
  ],
  standalone: true,
})
export class AppComponent {
  redState = '';
  greenState = '';
  blueState = '';
  orangeRotate = false;
  looping = false;

  animateSequence(loop: boolean) {
    this.looping = loop;
    this.redState = 'active'; // Start with red
  }

  onRedDone(event: AnimationEvent) {
    if (event.toState === 'active') {
      this.greenState = 'active'; // Start green
      setTimeout(() => {
        this.blueState = 'active'; // Blue starts 1s before green finishes
      }, 3000); // Green = 4s, blue starts after 3s
    }
  }

  onBlueDone(event: AnimationEvent) {
    if (event.toState === 'active' && this.looping) {
      // Reset states to restart
      this.redState = '';
      this.greenState = '';
      this.blueState = '';
      setTimeout(() => this.animateSequence(true), 0);
    }
  }

  playCssRotation() {
    this.orangeRotate = false;
    setTimeout(() => (this.orangeRotate = true), 0);
  }
}
