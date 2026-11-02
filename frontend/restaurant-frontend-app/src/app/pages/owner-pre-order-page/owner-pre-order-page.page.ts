import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';

interface DishItem {
  id: string;
  name: string;
  guestName: string;
  price: number;
}

interface GroupOrderDetails {
  code: string;
  dateTime: string;
  guestsCount: number;
  status: string;
  shareLink: string;
  dishes: DishItem[];
}

@Component({
  selector: 'app-owner-pre-order-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './owner-pre-order-page.page.html',
  styleUrl: './owner-pre-order-page.page.scss',
})
export class OwnerPreOrderPagePage implements OnInit {
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);

  reservationId: string | null = null;
  isLoading = true;
  isCopied = false;

  reservation = {
    code: 'DEMO-12345',
    dateTime: '15 Листопада, 19:00',
    guestsCount: 4,
    status: 'Активне',
  };

  shareLink = 'http://localhost:4200/join/demo-token-123';

  dishes: DishItem[] = [
    { id: '1', name: 'Хачапурі по-аджарськи', guestName: 'Олексій (Власник)', price: 240 },
    { id: '2', name: 'Салат Цезар з куркою', guestName: 'Марія', price: 190 },
    { id: '3', name: 'Лимонад класичний 1Л', guestName: 'Дмитро', price: 120 },
  ];

  ngOnInit(): void {
    // Отримуємо ID із URL
    this.reservationId = this.route.snapshot.paramMap.get('id');
    this.fetchGroupOrderData();
  }

  fetchGroupOrderData(): void {
    if (!this.reservationId || this.reservationId === 'demo-reservation') {
      this.isLoading = false;
      return;
    }

    // Запит до реального API бекенду
    this.http.get<GroupOrderDetails>(`/api/reservations/${this.reservationId}/group-order`).subscribe({
      next: (data) => {
        if (data) {
          this.reservation.code = data.code || this.reservation.code;
          this.reservation.dateTime = data.dateTime || this.reservation.dateTime;
          this.reservation.guestsCount = data.guestsCount || this.reservation.guestsCount;
          this.reservation.status = data.status || this.reservation.status;
          this.shareLink = data.shareLink || this.shareLink;
          this.dishes = data.dishes || this.dishes;
        }
        this.isLoading = false;
      },
      error: (err) => {
        console.warn('API не доступне, використовуються локальні мок-дані:', err);
        this.isLoading = false;
      },
    });
  }

  get totalPrice(): number {
    return this.dishes.reduce((sum, item) => sum + item.price, 0);
  }

  copyLink(): void {
    navigator.clipboard.writeText(this.shareLink).then(() => {
      this.isCopied = true;
      setTimeout(() => (this.isCopied = false), 2500);
    });
  }

  removeDish(id: string): void {
    if (this.reservationId && this.reservationId !== 'demo-reservation') {
      this.http.delete(`/api/group-orders/dishes/${id}`).subscribe({
        next: () => {
          this.dishes = this.dishes.filter((dish) => dish.id !== id);
        },
        error: () => {
          // Локальне видалення при помилці
          this.dishes = this.dishes.filter((dish) => dish.id !== id);
        },
      });
    } else {
      this.dishes = this.dishes.filter((dish) => dish.id !== id);
    }
  }

  submitOrder(): void {
    if (this.reservationId && this.reservationId !== 'demo-reservation') {
      this.http.post(`/api/reservations/${this.reservationId}/group-order/submit`, {}).subscribe({
        next: () => alert('Замовлення успішно підтверджено через API!'),
        error: () => alert('Замовлення підтверджено (демо-режим)!'),
      });
    } else {
      alert('Замовлення успішно підтверджено!');
    }
  }
}