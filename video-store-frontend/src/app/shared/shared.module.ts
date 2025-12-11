import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TruncatePipe } from './pipes/truncate.pipe';
import { LoadingSpinnerComponent } from './components/loading-spinner/loading-spinner';

@NgModule({
  declarations: [
    TruncatePipe,
    LoadingSpinnerComponent
  ],
  imports: [
    CommonModule,
    RouterModule
  ],
  exports: [
    CommonModule,
    RouterModule,
    TruncatePipe,
    LoadingSpinnerComponent
  ]
})
export class SharedModule { }
