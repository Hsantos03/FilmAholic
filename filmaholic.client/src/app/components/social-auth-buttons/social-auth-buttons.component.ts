import { Component, EventEmitter, Output } from '@angular/core';

/// <summary>
/// Component that renders social authentication buttons for Google and Facebook.
/// </summary>
@Component({
  selector: 'app-social-auth-buttons',
  templateUrl: './social-auth-buttons.component.html'
})
export class SocialAuthButtonsComponent {
  @Output() googleClick = new EventEmitter<void>();
  @Output() facebookClick = new EventEmitter<void>();
}
