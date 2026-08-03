// 管线类型
export interface PipeType {
  id: number;
  name: string;
  type: 'tube' | 'weak_cable' | 'strong_cable' | 'cable' | 'encoder' | 'other';
  diameter: number;
  weight: number;
  bendMultiplier: number;
}

// 活动管线（管线清单）
export interface ActivePipeBase {
  kind?: 'pipe' | 'module' | 'component';
  qty: number;
}

export interface ActivePipeItem extends ActivePipeBase {
  kind?: 'pipe';
  libId: number;
  name?: string;
  type?: string;
  diameter?: number;
  weight?: number;
  bendMultiplier?: number;
}

export interface ActivePipeModule extends ActivePipeBase {
  kind: 'module';
  moduleId: number;
}

export interface ActivePipeComponent extends ActivePipeBase {
  kind: 'component';
  componentId: number;
}

export type ActivePipe = ActivePipeItem | ActivePipeModule | ActivePipeComponent;

// 管线模块
export interface PipeModuleItem {
  id: number;
  moduleId: number;
  pipeTypeId: number;
  qty: number;
  layer: 'top' | 'bottom';
  pipeType?: PipeType;
}

export interface PipeModule {
  id: number;
  name: string;
  description: string;
  items: PipeModuleItem[];
}

// 管线元件
export interface PipeComponentItem {
  id: number;
  componentId: number;
  pipeTypeId: number;
  qty: number;
  layer: 'top' | 'bottom';
  pipeType?: PipeType;
}

export interface PipeComponent {
  id: number;
  name: string;
  description: string;
  items: PipeComponentItem[];
}
