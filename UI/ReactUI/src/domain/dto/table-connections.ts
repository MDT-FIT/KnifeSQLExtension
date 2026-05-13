import z from 'zod';

export const ForeignConstraintDtoSchema = z.object({
  TargetTable: z.string(),
  Mappings: z.array(
    z.object({
      Source: z.string(),
      Target: z.string(),
    }),
  ),
});

export type ForeignConstraintDto = z.infer<typeof ForeignConstraintDtoSchema>;
