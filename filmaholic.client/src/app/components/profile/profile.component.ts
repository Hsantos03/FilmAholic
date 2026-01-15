import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})
export class ProfileComponent implements OnInit {

  userName = 'RandomUser';
  joined = '14 hours ago';
  bio = 'Lorem ipsum dolor sit amet consectetur adipisicing elit. Quisque faucibus ex sapien vitae pellentesque sem placerat.';

  watchLater = [
    { cover: 'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTy05E5wZ05u6IyDDHomiYZE4aDSFPUTeHOX-2B03RGqjYUeLK7' },
    { cover: 'https://via.placeholder.com/80x120' },
    { cover: 'https://via.placeholder.com/80x120' }
  ];

  ngOnInit(): void { }
}
