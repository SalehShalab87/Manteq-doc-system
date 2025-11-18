import { Injectable, signal, computed, inject } from '@angular/core';
import { MANTEQ_CONFIG } from '../tokens/config.tokens';
import { ManteqLibConfig } from '../models/config.model';
import { CSS_VARIABLES, DEFAULT_THEME } from '../constants/theme.constants';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private config = inject(MANTEQ_CONFIG, { optional: true });
  
  // Signals for reactive theme state
  private configSignal = signal<ManteqLibConfig>(this.config || DEFAULT_THEME);
  
  // Computed values
  readonly primaryColor = computed(() => this.configSignal().primaryColor);
  readonly accentColor = computed(() => this.configSignal().accentColor);
  readonly fontStyle = computed(() => this.configSignal().fontStyle);
  readonly companyName = computed(() => this.configSignal().companyName);
  readonly companyLogo = computed(() => this.configSignal().companyLogo);
  readonly disclaimer = computed(() => this.configSignal().disclaimer);
  
  constructor() {
    this.applyTheme();
  }
  
  /**
   * Apply theme by setting CSS custom properties
   */
  private applyTheme(): void {
    const config = this.configSignal();
    const root = document.documentElement;
    
    // Base colors
    root.style.setProperty(CSS_VARIABLES.PRIMARY_COLOR, config.primaryColor);
    root.style.setProperty(CSS_VARIABLES.ACCENT_COLOR, config.accentColor);
    root.style.setProperty(CSS_VARIABLES.FONT_FAMILY, config.fontStyle);
    
    // Generate hover and light variations
    const primaryHover = this.adjustColorBrightness(config.primaryColor, -20);
    const accentHover = this.adjustColorBrightness(config.accentColor, -20);
    const primaryLight = this.adjustColorBrightness(config.primaryColor, 40);
    const accentLight = this.adjustColorBrightness(config.accentColor, 40);
    
    root.style.setProperty(CSS_VARIABLES.PRIMARY_HOVER, primaryHover);
    root.style.setProperty(CSS_VARIABLES.ACCENT_HOVER, accentHover);
    root.style.setProperty(CSS_VARIABLES.PRIMARY_LIGHT, primaryLight);
    root.style.setProperty(CSS_VARIABLES.ACCENT_LIGHT, accentLight);
  }
  
  /**
   * Adjust color brightness (positive = lighter, negative = darker)
   */
  private adjustColorBrightness(color: string, percent: number): string {
    const num = parseInt(color.replace('#', ''), 16);
    const r = Math.min(255, Math.max(0, (num >> 16) + percent));
    const g = Math.min(255, Math.max(0, ((num >> 8) & 0x00FF) + percent));
    const b = Math.min(255, Math.max(0, (num & 0x0000FF) + percent));
    return `#${(0x1000000 + (r << 16) + (g << 8) + b).toString(16).slice(1)}`;
  }
  
  /**
   * Update theme configuration (optional - for dynamic theme changes)
   */
  updateConfig(config: Partial<ManteqLibConfig>): void {
    this.configSignal.update(current => ({ ...current, ...config }));
    this.applyTheme();
  }

  /**
   * Set entire theme configuration (for parent app runtime updates)
   */
  setConfig(config: ManteqLibConfig): void {
    this.configSignal.set(config);
    this.applyTheme();
  }

  /**
   * Get current configuration (for debugging or parent app access)
   */
  getCurrentConfig(): ManteqLibConfig {
    return this.configSignal();
  }
}
