import { TestBed } from '@angular/core/testing';
import { CartService } from './cart.service';
import { Product } from '../../shared/models/product.model';

function makeProduct(overrides: Partial<Product> = {}): Product {
  return {
    id: 1,
    name: 'Test Product',
    description: 'A product',
    price: 10,
    inStock: true,
    images: [],
    categoryId: 1,
    categoryName: 'Category',
    ...overrides
  };
}

describe('CartService', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('starts empty', () => {
    const service = TestBed.inject(CartService);
    expect(service.items()).toEqual([]);
    expect(service.itemCount()).toBe(0);
  });

  it('addItem() adds a new item at the requested quantity', () => {
    const service = TestBed.inject(CartService);
    service.addItem(makeProduct(), 2);

    expect(service.items().length).toBe(1);
    expect(service.items()[0].quantity).toBe(2);
  });

  it('addItem() on an existing item adds to the existing quantity', () => {
    const service = TestBed.inject(CartService);
    const product = makeProduct();

    service.addItem(product, 3);
    service.addItem(product, 4);

    expect(service.items().length).toBe(1);
    expect(service.items()[0].quantity).toBe(7);
  });

  it('updateQuantity() sets the quantity directly', () => {
    const service = TestBed.inject(CartService);
    const product = makeProduct();
    service.addItem(product, 1);

    service.updateQuantity(product.id, 10);

    expect(service.items()[0].quantity).toBe(10);
  });

  it('updateQuantity() with zero or below removes the item', () => {
    const service = TestBed.inject(CartService);
    const product = makeProduct();
    service.addItem(product, 1);

    service.updateQuantity(product.id, 0);

    expect(service.items()).toEqual([]);
  });

  it('removeItem() removes only the matching item', () => {
    const service = TestBed.inject(CartService);
    const productA = makeProduct({ id: 1 });
    const productB = makeProduct({ id: 2 });
    service.addItem(productA, 1);
    service.addItem(productB, 1);

    service.removeItem(productA.id);

    expect(service.items().length).toBe(1);
    expect(service.items()[0].product.id).toBe(productB.id);
  });

  it('itemCount() sums quantities across items', () => {
    const service = TestBed.inject(CartService);
    service.addItem(makeProduct({ id: 1 }), 2);
    service.addItem(makeProduct({ id: 2 }), 3);

    expect(service.itemCount()).toBe(5);
  });

  it('totalPrice() sums price * quantity across items', () => {
    const service = TestBed.inject(CartService);
    service.addItem(makeProduct({ id: 1, price: 10 }), 2);
    service.addItem(makeProduct({ id: 2, price: 5 }), 1);

    expect(service.totalPrice()).toBe(25);
  });

  it('clear() empties the cart', () => {
    const service = TestBed.inject(CartService);
    service.addItem(makeProduct(), 1);

    service.clear();

    expect(service.items()).toEqual([]);
  });
});
