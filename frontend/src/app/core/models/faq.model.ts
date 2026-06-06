export interface FAQ {
  id: string;
  question: string;
  answer: string;
  category: string;
  country: 'CL' | 'BO' | 'ALL';
  order: number;
}
