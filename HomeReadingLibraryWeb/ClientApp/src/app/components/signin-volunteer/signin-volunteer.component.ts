import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { Router } from '@angular/router';
import { ClassVolunteer } from './../../entities/class-volunteer';
import { ClassWithVolunteers } from './../../entities/class-with-volunteers';
import { Component, OnInit, TemplateRef } from '@angular/core';
import { BaggyBookService } from '../../services/baggy-book.service';

import { DayOfWeek } from '../../entities/day-of-week.enum';

@Component({
  selector: 'app-signin-volunteer',
  templateUrl: './signin-volunteer.component.html',
  styleUrls: ['./signin-volunteer.component.css']
})
export class SigninVolunteerComponent implements OnInit {

  classes: ClassWithVolunteers[];
  public modalRef: BsModalRef;

  constructor(
    private baggyBookService: BaggyBookService,
    private router: Router,
    private modelService: BsModalService
  ) { }

  getDayOfWeek(volunteer: ClassVolunteer): string {
    return DayOfWeek[volunteer.dayOfWeek];
  }

  ngOnInit() {
    this.baggyBookService.getClassesWithVolunteers()
      .then(classes => this.classes = classes);
  }

  loginClicked(volunteerId: string) {
    this.baggyBookService.loginVolunteer(volunteerId)
      .then(() => this.router.navigate(['/checkin']))
      .catch(error => alert(error));
  }

  adminLoginClicked(content: TemplateRef<any>) {
    this.modalRef = this.modelService.show(content);
  }

  adminLoggedIn() {
    this.modalRef.hide();
  }

}
