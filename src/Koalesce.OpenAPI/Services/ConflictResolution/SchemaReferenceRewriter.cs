namespace Koalesce.OpenAPI.Services.ConflictResolution;

/// <summary>
/// Service responsible for rewriting Schema reference Ids in OpenAPI documents.
/// </summary>
internal static class SchemaReferenceRewriter
{
	/// <summary>
	/// Rewrites all schema references in the document according to the rename map.
	/// </summary>
	public static void RewriteReferences(OpenApiDocument document, IReadOnlyDictionary<string, string> renames)
	{
		if (renames.Count == 0) 
			return;

		if (document.Paths is null) 
			return;

		foreach (var pathItem in document.Paths.Values)
		{
			if (pathItem.Operations is null) 
				continue;

			foreach (var operation in pathItem.Operations.Values)
			{
				// Rewrite request body schema references
				RewriteRequestBody(operation.RequestBody, renames, document);

				// Rewrite response schema references
				if (operation.Responses is not null)
				{
					foreach (var response in operation.Responses.Values)
						RewriteResponse(response, renames, document);
				}

				// Rewrite parameter schema references
				RewriteParameters(operation.Parameters, renames, document);
			}

			// Rewrite path-level parameter schema references
			RewriteParameters(pathItem.Parameters, renames, document);
		}

		// Rewrite schema references within component schemas (nested refs)
		if (document.Components?.Schemas is not null)
		{
			foreach (var kvp in document.Components.Schemas.ToList())
			{
				var rewrittenSchema = RewriteSchemaDeep(kvp.Value, renames, document);
				if (!ReferenceEquals(rewrittenSchema, kvp.Value))
					document.Components.Schemas[kvp.Key] = rewrittenSchema;
			}
		}
	}

	/// <summary>
	/// Rewrites the schemas within the content of the specified OpenAPI request body according to the provided property
	/// renaming map.
	/// </summary>	
	private static void RewriteRequestBody(
		IOpenApiRequestBody? requestBody, 
		IReadOnlyDictionary<string, string> renames, 
		OpenApiDocument document)
	{
		if (requestBody?.Content is null) 
			return;

		foreach (var kvp in requestBody.Content.ToList())
		{
			var mediaType = kvp.Value;
			if (mediaType.Schema is null) 
				continue;

			// Use RewriteSchemaDeep to handle nested references
			var newSchema = RewriteSchemaDeep(mediaType.Schema, renames, document);
			if (!ReferenceEquals(newSchema, mediaType.Schema))
				mediaType.Schema = newSchema;
		}
	}

	/// <summary>
	/// Rewrites the schemas in the response content to reflect renamed components according to the provided mapping.
	/// </summary>	
	private static void RewriteResponse(
		IOpenApiResponse response, 
		IReadOnlyDictionary<string, string> renames, 
		OpenApiDocument document)
	{
		if (response.Content is null) 
			return;

		foreach (var kvp in response.Content.ToList())
		{
			var mediaType = kvp.Value;
			if (mediaType.Schema is null) 
				continue;

			// Use RewriteSchemaDeep to handle nested references
			var newSchema = RewriteSchemaDeep(mediaType.Schema, renames, document);
			if (!ReferenceEquals(newSchema, mediaType.Schema))
				mediaType.Schema = newSchema;
		}
	}

	/// <summary>
	/// Rewrites the schemas of the specified OpenAPI parameters by applying the provided renaming rules.
	/// </summary>	
	private static void RewriteParameters(
		IList<IOpenApiParameter>? parameters, 
		IReadOnlyDictionary<string, string> renames, 
		OpenApiDocument document)
	{
		if (parameters is null) 
			return;

		foreach (var param in parameters)
		{
			if (param is OpenApiParameter concreteParam && concreteParam.Schema is not null)
			{
				// Use RewriteSchemaDeep to handle nested references
				var newSchema = RewriteSchemaDeep(concreteParam.Schema, renames, document);
				if (!ReferenceEquals(newSchema, concreteParam.Schema))
					concreteParam.Schema = newSchema;
			}
		}
	}

	/// <summary>
	/// Recursively rewrites schema references, returning a potentially new schema.
	/// </summary>
	private static IOpenApiSchema RewriteSchemaDeep(
		IOpenApiSchema schema,
		IReadOnlyDictionary<string, string> renames,
		OpenApiDocument document)
	{
		// If it's a reference that needs renaming, create a new reference
		if (schema is OpenApiSchemaReference schemaRef)
		{
			var newRef = TryCreateRenamedReference(schemaRef, renames);
			return newRef ?? schema;
		}

		// If it's a concrete schema, recursively process nested schemas
		if (schema is OpenApiSchema concreteSchema)
		{
			// Items (for arrays)
			if (concreteSchema.Items is not null)
			{
				var newItems = RewriteSchemaDeep(concreteSchema.Items, renames, document);
				if (!ReferenceEquals(newItems, concreteSchema.Items))
					concreteSchema.Items = newItems;
			}

			// Properties (for objects)
			if (concreteSchema.Properties is not null)
			{
				foreach (var kvp in concreteSchema.Properties.ToList())
				{
					var newProp = RewriteSchemaDeep(kvp.Value, renames, document);
					if (!ReferenceEquals(newProp, kvp.Value))
						concreteSchema.Properties[kvp.Key] = newProp;
				}
			}

			// AllOf
			RewriteSchemaList(concreteSchema.AllOf, renames, document);

			// OneOf
			RewriteSchemaList(concreteSchema.OneOf, renames, document);

			// AnyOf
			RewriteSchemaList(concreteSchema.AnyOf, renames, document);

			// AdditionalProperties
			if (concreteSchema.AdditionalProperties is IOpenApiSchema additionalSchema)
			{
				var newAdditional = RewriteSchemaDeep(additionalSchema, renames, document);
				if (!ReferenceEquals(newAdditional, additionalSchema))
					concreteSchema.AdditionalProperties = newAdditional;
			}
		}

		return schema;
	}

	/// <summary>
	/// Rewrites each schema in the specified list by applying the provided renaming rules to schema references within the
	/// context of the given OpenAPI document.
	/// </summary>	
	private static void RewriteSchemaList(
		IList<IOpenApiSchema>? schemas,
		IReadOnlyDictionary<string, string> renames,
		OpenApiDocument document)
	{
		if (schemas is null) 
			return;

		for (int i = 0; i < schemas.Count; i++)
		{
			var newSchema = RewriteSchemaDeep(schemas[i], renames, document);
			if (!ReferenceEquals(newSchema, schemas[i]))
				schemas[i] = newSchema;
		}
	}

	/// <summary>
	/// Creates a new OpenApiSchemaReference with the renamed Id if the schema needs renaming.
	/// Returns null if no renaming is needed.
	/// </summary>
	private static OpenApiSchemaReference? TryCreateRenamedReference(
		IOpenApiSchema schema,
		IReadOnlyDictionary<string, string> renames)
	{
		if (schema is not OpenApiSchemaReference schemaRef)
			return null;

		var reference = schemaRef.Reference;
		if (reference?.Id is null)
			return null;

		if (!renames.TryGetValue(reference.Id, out var newId))
			return null;

		// Create a new reference with the renamed Id
		return new OpenApiSchemaReference(newId);
	}
}