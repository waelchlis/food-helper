import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatBadgeModule } from '@angular/material/badge';
import { MatTabsModule } from '@angular/material/tabs';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';

interface Product {
  id: number;
  name: string;
  category: string;
  price: number;
  description: string;
}

interface CartItem {
  product: Product;
  quantity: number;
}

@Component({
  selector: 'app-sample-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatTableModule,
    MatIconModule,
    MatToolbarModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatBadgeModule,
    MatTabsModule,
    MatChipsModule,
    MatDividerModule,
  ],
  templateUrl: './sample-page.html',
  styleUrl: './sample-page.scss',
})
export class SamplePage {
  selectedCategory = signal<string>('all');
  cartItems = signal<CartItem[]>([]);
  showCart = signal<boolean>(false);

  categories = [
    { id: 'all', name: 'All Products' },
    { id: 'fruits', name: 'Fruits' },
    { id: 'vegetables', name: 'Vegetables' },
    { id: 'legumes', name: 'Legumes' },
    { id: 'meat', name: 'Meat' },
    { id: 'dairy', name: 'Dairy' },
  ];

  products: Product[] = [
    { id: 1, name: 'Organic Apples', category: 'fruits', price: 5.99, description: 'Fresh red apples' },
    { id: 2, name: 'Bananas', category: 'fruits', price: 3.49, description: 'Golden ripe bananas' },
    { id: 3, name: 'Berries Mix', category: 'fruits', price: 8.99, description: 'Strawberries, blueberries, raspberries' },
    { id: 4, name: 'Carrots', category: 'vegetables', price: 2.99, description: 'Fresh orange carrots' },
    { id: 5, name: 'Broccoli', category: 'vegetables', price: 4.49, description: 'Organic green broccoli' },
    { id: 6, name: 'Tomatoes', category: 'vegetables', price: 3.99, description: 'Ripe vine tomatoes' },
    { id: 7, name: 'Lentils', category: 'legumes', price: 6.99, description: 'Red lentils 1kg bag' },
    { id: 8, name: 'Chickpeas', category: 'legumes', price: 5.49, description: 'Dried chickpeas 1kg bag' },
    { id: 9, name: 'Black Beans', category: 'legumes', price: 4.99, description: 'High-protein black beans' },
    { id: 10, name: 'Premium Beef', category: 'meat', price: 15.99, description: 'Grass-fed beef steaks (per lb)' },
    { id: 11, name: 'Free-Range Chicken', category: 'meat', price: 10.49, description: 'Organic chicken breast (per lb)' },
    { id: 12, name: 'Wild-Caught Salmon', category: 'meat', price: 18.99, description: 'Fresh salmon fillets (per lb)' },
    { id: 13, name: 'Greek Yogurt', category: 'dairy', price: 6.49, description: '500g container' },
    { id: 14, name: 'Artisan Cheese', category: 'dairy', price: 9.99, description: 'Mixed aged cheese selection' },
    { id: 15, name: 'Raw Milk', category: 'dairy', price: 7.99, description: 'Fresh raw milk 1L' },
  ];

  filteredProducts = computed(() => {
    const category = this.selectedCategory();
    if (category === 'all') {
      return this.products;
    }
    return this.products.filter(p => p.category === category);
  });

  cartTotal = computed(() => {
    return this.cartItems().reduce((total, item) => total + item.product.price * item.quantity, 0);
  });

  cartCount = computed(() => {
    return this.cartItems().reduce((count, item) => count + item.quantity, 0);
  });

  addToCart(product: Product): void {
    const cartItems = this.cartItems();
    const existingItem = cartItems.find(item => item.product.id === product.id);

    if (existingItem) {
      existingItem.quantity++;
    } else {
      cartItems.push({ product, quantity: 1 });
    }

    this.cartItems.set([...cartItems]);
  }

  removeFromCart(productId: number): void {
    const cartItems = this.cartItems().filter(item => item.product.id !== productId);
    this.cartItems.set(cartItems);
  }

  updateQuantity(productId: number, quantity: number): void {
    if (quantity <= 0) {
      this.removeFromCart(productId);
      return;
    }

    const cartItems = this.cartItems();
    const item = cartItems.find(i => i.product.id === productId);
    if (item) {
      item.quantity = quantity;
      this.cartItems.set([...cartItems]);
    }
  }

  clearCart(): void {
    this.cartItems.set([]);
  }
}
