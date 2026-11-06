import { Component, computed, inject } from '@angular/core';
import { QRCodeComponent } from 'angularx-qrcode';
import { GAME_API } from '../core/game-api';

@Component({
  selector: 'app-join-qr',
  imports: [QRCodeComponent],
  template: `
    @if (joinUrl(); as url) {
      <qrcode
        [qrdata]="url"
        [width]="200"
        [margin]="1"
        errorCorrectionLevel="M"
        colorDark="#0A0505"
        colorLight="#F5E6C8"
      />
      <p>{{ displayUrl() }}</p>
    }
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.5vh;
    }

    qrcode {
      display: block;
      width: 11vw;
      max-width: 22vh;
      border: 2px solid var(--gold);
      border-radius: 6px;
      line-height: 0;
      overflow: hidden;
    }

    qrcode ::ng-deep canvas,
    qrcode ::ng-deep img {
      width: 100% !important;
      height: auto !important;
    }

    p {
      margin: 0;
      color: var(--champagne);
      font-size: clamp(0.7rem, 0.85vw, 1rem);
      overflow-wrap: anywhere;
      text-align: center;
    }
  `,
})
export class JoinQr {
  private readonly api = inject(GAME_API);

  protected readonly joinUrl = computed(() => this.api.config()?.joinUrl);
  protected readonly displayUrl = computed(() => this.joinUrl()?.replace(/^https?:\/\//, ''));
}
