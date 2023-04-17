import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { PurchaseRequest } from './PurchaseRequest';

@Component({
  selector: 'app-purchase',
  templateUrl: './purchase.component.html',
  styleUrls: ['./purchase.component.css']
})
export class PurchaseComponent {
  name: string = '';
  creditCardNumber: string = '';
  soda: number = 0;
  candyBar: number = 0;
  chips: number = 0;
  purchaseStatus: boolean = false;
  purchaseErrorMessage: string = '';

  constructor(private http: HttpClient) {

  }

  ngOnInit() {
    this.http.get<string>('/purchases').subscribe(
      (purchases: string) => {
        console.log(purchases)
      },
      (error: any) => {
        console.error('Error fetching purchases', error);
      }
    );
  }

  onSubmit(): void {
    if (this.chips == 0 && this.soda == 0 && this.candyBar == 0) {
      this.purchaseStatus = false;
      this.purchaseErrorMessage = 'Please buy something.'
      console.log("invalid items")
      return;
    }

    if (this.creditCardNumber == '' || this.name == '') {
      this.purchaseStatus = false;
      console.log("invalide credit")
      this.purchaseErrorMessage = 'Please input a valide credit card.'
      return;
    }

    const data: PurchaseRequest = {
      Soda: this.soda,
      CandyBar: this.candyBar,
      Chips: this.chips,
      Name: this.name,
      CreditCardNumber: this.creditCardNumber,
    };

    this.http.post('/purchases', data).subscribe(
      (response: any) => {
        console.log('Purchase successful');
        this.purchaseStatus = true;
        this.purchaseErrorMessage = '';
      },
      (error: any) => {
        console.error('Purchase failed', error);
        this.purchaseStatus = false;
      }
    );
  }
}
