import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {

  userName = localStorage.getItem('userName') || 'RandomUser';  
  joined = '14 hours ago';
  bio = 'Lorem ipsum dolor sit amet consectetur adipisicing elit. Quisque faucibus ex sapien vitae pellentesque sem placerat.';

  watchLater = [
    { cover: 'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTy05E5wZ05u6IyDDHomiYZE4aDSFPUTeHOX-2B03RGqjYUeLK7' },
    { cover: 'https://via.placeholder.com/80x120' },
    { cover: 'https://via.placeholder.com/80x120' }
  ];

  private apiBase = 'https://localhost:7277/api/Profile';

  // Modal / edit state
  isEditing = false;
  editUserName = '';
  editBio = '';

  constructor(private http: HttpClient) { }

  ngOnInit(): void {
    const userId = localStorage.getItem('user_id');
    if (!userId) {
      console.warn('No user_id in localStorage — using fallback values.');
      return;
    }

    // Call backend GET api/Profile/{id} to fetch user data
    this.http.get<any>(`${this.apiBase}/${encodeURIComponent(userId)}`, { withCredentials: true }).subscribe({
      next: (res) => {
        // Backend returns fields like: id, userName, nome, sobrenome, email, dataCriacao
        this.userName = res?.userName;

        if (res?.dataCriacao) {
          // Normalize server date to readable string
          this.joined = new Date(res.dataCriacao).toLocaleString();
        }

        if (res?.bio) {
          this.bio = res.bio;
        }
      },
      error: (err) => {
        console.warn('Failed to load profile from API; keeping local values.', err);
      }
    });
  }

  openEdit(): void {
    // populate the edit fields with current values
    this.editUserName = this.userName;
    this.editBio = this.bio;
    this.isEditing = true;
  }

  closeEdit(): void {
    this.isEditing = false;
  }

  saveChanges(): void {
    // Update UI immediately
    this.userName = this.editUserName;
    this.bio = this.editBio;
    this.isEditing = false;

    // Optionally persist to server if an update endpoint exists.
    const userId = localStorage.getItem('user_id');
    if (!userId) return;

    this.http.put<any>(`${this.apiBase}/${encodeURIComponent(userId)}`, { userName: this.userName, bio: this.bio }, { withCredentials: true }).subscribe({
      next: () => {
        // success - nothing else required for now
      },
      error: (err) => {
        console.warn('Failed to persist profile changes to API.', err);
      }
    });
  }
}
