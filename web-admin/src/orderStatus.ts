export enum OrderStatus {
    Draft = 0,
    Pending = 1,
    Accepted = 2,
    Preparing = 3,
    Ready = 4,
    Completed = 5,
    Cancelled = 6,
  }
  
  export function orderStatusLabel(s: number | string) {
    if (typeof s === "string") return s;
    return OrderStatus[s] ?? String(s);
  }