interface InvoiceModel {
  shopName:    string;
  shopAddress: string;
  shopPhone:   string;
  invoiceNo:   string;
  date:        string;
  customer:    string;
  phone:       string;
  status:      'Paid' | 'Udhar' | 'Pending';
  discount:    number;
  taxRate:     number;
  paymentMethod: string;
  items: InvoiceItem[];
}