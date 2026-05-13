import z from 'zod';

export const ColumnSchemaDtoSchema = z.object({
  Name: z.string(),
  SqlType: z.string(),
  MaxLength: z.number().nullable(),
  IsNullable: z.boolean(),
  IsPrimaryKey: z.boolean(),
  IsIdentity: z.boolean(),
  IsComputed: z.boolean(),
  IsUnique: z.boolean(),
  HasDefault: z.boolean(),
  IsPlain: z.boolean(),
});

export type ColumnSchemaDto = z.infer<typeof ColumnSchemaDtoSchema>;
