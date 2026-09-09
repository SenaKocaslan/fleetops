export interface AtamaSonucu {
  taskId: string;
  materialCode: string;
  priority: number;
  agvId: string;
  agvCode: string;
}

export interface OtomatikAtamaOzeti {
  atananlar: AtamaSonucu[];
  bekleyenGorev: number;
  musaitAgv: number;
  strateji: string;
}
