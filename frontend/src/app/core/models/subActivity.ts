interface SubActivity {
  id:          number;
  business:    string;
  owner:       string;
  avatarColor: string;
  plan:        'Free' | 'Basic' | 'Premium';
  start:       string;
  expiry:      string;
  daysLeft:    number;
  amount:      number;
  status:      'Active' | 'Expired' | 'Cancelled';
}