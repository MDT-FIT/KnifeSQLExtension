import type { Edge, Node } from 'reactflow';

export type DatabaseSchemaNodeData = {
  data: {
    label: string;
    schema: { title: string; type: string }[];
  };
};

export interface DatabaseFlowSchema {
  nodes: Node<DatabaseSchemaNodeData['data']>[];
  edges: Edge[];
}

export const DatabaseFlowSchema = {
  create: (init: Partial<DatabaseFlowSchema> = {}) => {
    return {
      ...init,
      edges: init.edges ?? [],
      nodes: init.nodes ?? [],
    };
  },
};
