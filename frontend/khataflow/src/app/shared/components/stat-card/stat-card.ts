import { NgClass } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-stat-card',
  imports: [NgClass],
  templateUrl: './stat-card.html',
  styleUrl: './stat-card.css',
})
export class StatCard {
  @Input() title!: string;
  @Input() value!: string;         
  @Input() icon!: string;            
  @Input() iconBg: string = 'bg-orange-50';    
  @Input() iconColor: string = 'text-orange-500'; 
  @Input() badgeText: string = '';   
  @Input() badgeType: 'up' | 'down' | 'neutral' = 'neutral';
}
