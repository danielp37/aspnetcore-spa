import { BookCopyReservationWithData } from './../../entities/book-copy-reservation-with-data';
import { BaggyBookService } from './../../services/baggy-book.service';
import { BookCopyReservation } from './../../entities/book-copy-reservation';
import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-book-copy-reservations',
  templateUrl: './book-copy-reservations.component.html',
  styleUrls: ['./book-copy-reservations.component.css']
})
export class BookCopyReservationsComponent implements OnInit {

  bookCopyReservations: BookCopyReservationWithData[];

  constructor(
    private baggyBookService: BaggyBookService
  ) { }

  ngOnInit() {
    this.baggyBookService.getBookCopyReservations()
      .then(bcr =>
        this.bookCopyReservations = bcr
      );
  }

}
