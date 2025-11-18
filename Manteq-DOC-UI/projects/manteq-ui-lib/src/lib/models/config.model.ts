/**
 * Library configuration interface
 */
export interface ManteqLibConfig {
  companyName: string;
  companyLogo: string | null;
  primaryColor: string;
  accentColor: string;
  fontStyle: string;
  disclaimer: string;
}

/**
 * User context interface
 */
export interface UserContext {
  email: string;
  name: string;
}
