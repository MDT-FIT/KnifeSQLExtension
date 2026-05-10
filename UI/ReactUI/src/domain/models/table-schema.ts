import z from 'zod';
import { TableColumnsSchema } from './table-columns';
import { TableConnectionsSchema } from './table-connections';

export const TableSchemaSchema = z.object({
  name: z.string(),
  columns: z.array(TableColumnsSchema),
  connections: z.array(TableConnectionsSchema),
});

export type TableSchema = z.infer<typeof TableSchemaSchema>;
