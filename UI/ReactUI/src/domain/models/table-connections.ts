import z from 'zod';

export const ConnectionsMappingSchema = z.object({
  source: z.string(),
  target: z.string(),
});

export const TableConnectionsSchema = z.object({
  target: z.string(),
  mappings: z.array(ConnectionsMappingSchema),
});

export type TableConnections = z.infer<typeof TableConnectionsSchema>;
export type ConnectionsMapping = z.infer<typeof ConnectionsMappingSchema>;
