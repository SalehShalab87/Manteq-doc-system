/**
 * Default theme configuration - fallback values if parent app doesn't provide config
 */
export const DEFAULT_THEME = {
  companyName: 'Doc & Email Portal',
  companyLogo: null,
  primaryColor: '#2c3e50',
  accentColor: '#3498db',
  fontStyle: 'Inter, system-ui, sans-serif',
  disclaimer: 'By Manteq'
} as const;

/**
 * CSS Custom Property names
 */
export const CSS_VARIABLES = {
  PRIMARY_COLOR: '--manteq-primary-color',
  ACCENT_COLOR: '--manteq-accent-color',
  FONT_FAMILY: '--manteq-font-family',
  PRIMARY_HOVER: '--manteq-primary-hover',
  ACCENT_HOVER: '--manteq-accent-hover',
  PRIMARY_LIGHT: '--manteq-primary-light',
  ACCENT_LIGHT: '--manteq-accent-light'
} as const;
