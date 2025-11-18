import { Injectable } from '@angular/core';
import Swal, { SweetAlertIcon } from 'sweetalert2';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  
  /**
   * Show success toast
   */
  success(message: string, title: string = 'Success'): void {
    this.showToast('success', title, message);
  }
  
  /**
   * Show error toast
   */
  error(message: string, title: string = 'Error'): void {
    this.showToast('error', title, message);
  }
  
  /**
   * Show warning toast
   */
  warning(message: string, title: string = 'Warning'): void {
    this.showToast('warning', title, message);
  }
  
  /**
   * Show info toast
   */
  info(message: string, title: string = 'Info'): void {
    this.showToast('info', title, message);
  }
  
  /**
   * Show confirmation dialog
   */
  async confirm(
    title: string,
    message: string,
    confirmButtonText: string = 'Yes',
    cancelButtonText: string = 'Cancel'
  ): Promise<boolean> {
    const result = await Swal.fire({
      title,
      text: message,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText,
      cancelButtonText,
      confirmButtonColor: 'var(--manteq-primary-color, #2c3e50)',
      cancelButtonColor: '#6c757d',
      reverseButtons: true
    });
    
    return result.isConfirmed;
  }
  
  /**
   * Show loading spinner
   */
  showLoading(message: string = 'Loading...'): void {
    Swal.fire({
      title: message,
      allowOutsideClick: false,
      allowEscapeKey: false,
      didOpen: () => {
        Swal.showLoading();
      }
    });
  }
  
  /**
   * Close loading spinner
   */
  hideLoading(): void {
    Swal.close();
  }
  
  /**
   * Show toast notification
   */
  private showToast(icon: SweetAlertIcon, title: string, text: string): void {
    const Toast = Swal.mixin({
      toast: true,
      position: 'top-end',
      showConfirmButton: false,
      timer: 3000,
      timerProgressBar: true,
      didOpen: (toast) => {
        toast.addEventListener('mouseenter', Swal.stopTimer);
        toast.addEventListener('mouseleave', Swal.resumeTimer);
      }
    });
    
    Toast.fire({
      icon,
      title,
      text
    });
  }
}
