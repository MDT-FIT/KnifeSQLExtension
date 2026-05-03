import type { Edge, Node } from 'reactflow';
import z from 'zod';

import type { DatabaseFlowSchema, DatabaseSchemaNodeData } from '../models/node';
import { TableSchemaSchema } from '../models/table-schema';

export const toNode = z.array(TableSchemaSchema).transform((tables): DatabaseFlowSchema => {
  const nodes: Node<DatabaseSchemaNodeData['data']>[] = [];
  const edges: Edge[] = [];

  let xOffset = 0;
  let yOffset = 0;

  tables.forEach((table, index) => {
    nodes.push({
      id: table.name,
      type: 'databaseSchema',
      position: { x: xOffset, y: yOffset },
      data: {
        label: table.name,
        schema: table.columns.map((col) => ({
          title: col.name,
          type: col.sqlType,
        })),
      },
    });

    // Simple grid positioning (optional: replace with dagre.js later)
    xOffset += 350;
    if ((index + 1) % 3 === 0) {
      xOffset = 0;
      yOffset += 400;
    }

    table.connections.forEach((connection) => {
      connection.mappings.forEach((mapping) => {
        edges.push({
          id: `edge-${table.name}-${connection.target}-${mapping.source}`,
          source: table.name,
          target: connection.target,
          sourceHandle: mapping.source,
          targetHandle: mapping.target,
          type: 'smoothstep',
          animated: false,
        });
      });
    });
  });

  return { nodes, edges };
});

export const ReactFlowNodeMapper = {
  toNode,
};
