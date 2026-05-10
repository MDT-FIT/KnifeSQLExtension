import z from 'zod';

export const TableColumnsSchema = z.object({
  name: z.string(),
  sqlType: z.string(),
  maxLength: z.number().nullable(),
  isNullable: z.boolean(),
  isPrimaryKey: z.boolean(),
  isIdentity: z.boolean(),
  isComputed: z.boolean(),
  isUnique: z.boolean(),
  isPlain: z.boolean(),
  hasDefault: z.boolean(),
});

export type TableColumns = z.infer<typeof TableColumnsSchema>;
