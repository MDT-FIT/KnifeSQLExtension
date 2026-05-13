import z from 'zod';
import { ColumnSchemaDtoSchema } from './table-columns';
import { ForeignConstraintDtoSchema } from './table-connections';

export const TableSchemaDtoSchema = z.object({
  Name: z.string(),
  Columns: z.array(ColumnSchemaDtoSchema),
  Connections: z.array(ForeignConstraintDtoSchema),
});

export type TableSchemaDto = z.infer<typeof TableSchemaDtoSchema>;
