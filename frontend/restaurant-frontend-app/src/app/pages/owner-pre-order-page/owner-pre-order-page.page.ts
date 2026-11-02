
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

interface DishItem {
  id: string;
  name: string;
  guestName: string;
  price: number;
}

@Component({
  selector: 'app-owner-pre-order-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './owner-pre-order-page.page.html',
  styleUrl: './owner-pre-order-page.page.scss',
})
export class OwnerPreOrderPagePage {
  // Мок-дані для бронювання
  reservation = {
    code: 'DEMO-12345',
    dateTime: '15 Листопада, 19:00',
    guestsCount: 4,
    status: 'Активне',
  };

  shareLink = 'http://localhost:4200/join/demo-token-123';
  isCopied = false;

  // Список страв
  dishes: DishItem[] = [
    { id: '1', name: 'Хачапурі по-аджарськи', guestName: 'Олексій (Власник)', price: 240 },
    { id: '2', name: 'Салат Цезар з куркою', guestName: 'Марія', price: 190 },
    { id: '3', name: 'Лимонад класичний 1Л', guestName: 'Дмитро', price: 120 },
  ];

  // Обчислення загальної суми
  get totalPrice(): number {
    return this.dishes.reduce((sum, item) => sum + item.price, 0);
  }

  // Копіювання посилання
  copyLink(): void {
    navigator.clipboard.writeText(this.shareLink).then(() => {
      this.isCopied = true;
      setTimeout(() => (this.isCopied = false), 2500);
    });
  }

  // Видалення страви
  removeDish(id: string): void {
    this.dishes = this.dishes.filter((dish) => dish.id !== id);
  }

  // Підтвердження замовлення
  submitOrder(): void {
    alert('Замовлення успішно підтверджено!');
  }
}