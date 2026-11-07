var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
app.UseCors();

var categorias = new[] { "Construção", "Elétrica", "Hidráulica", "Pintura", "Limpeza", "Fretes", "Jardinagem", "Reparos" };
var ofertas = new List<Oferta>
{
    new(1,"João da Silva","Pedreiro • Alvenaria e reformas","Construção",180,"diária",4.9,38,2.3,"Faço muros, reboco, pisos e pequenas reformas com capricho."),
    new(2,"Marcos Oliveira","Eletricista residencial","Elétrica",90,"visita",4.8,24,3.1,"Instalações, chuveiros, tomadas e manutenção elétrica residencial."),
    new(3,"Ana Paula Santos","Diarista • limpeza caprichada","Limpeza",160,"diária",5,41,1.8,"Limpeza completa para sua casa ficar cheirosa e organizada."),
    new(4,"Roberto Lima","Encanador • caça-vazamento","Hidráulica",120,"visita",4.7,31,4.5,"Conserto vazamentos, torneiras, descargas e instalação hidráulica."),
    new(5,"Carlos Mendes","Pintor residencial e comercial","Pintura",18,"m²",4.9,18,5.2,"Pintura interna, externa e texturas com entrega limpa."),
    new(6,"Diego Ferreira","Fretes e pequenas mudanças","Fretes",100,"viagem",4.8,27,3.7,"Fretes rápidos, mudanças pequenas e entregas."),
    new(7,"Raimundo Costa","Jardineiro • poda e manutenção","Jardinagem",130,"visita",4.9,16,6,"Poda, limpeza, plantio, grama e manutenção de jardins."),
    new(8,"Lucas Rocha","Marido de aluguel • reparos","Reparos",80,"hora",4.6,12,2.9,"Montagem de móveis e reparos do dia a dia.")
};

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/categorias", () => Results.Ok(categorias));
app.MapGet("/api/ofertas", (string? busca, string? categoria, string? ordem) =>
{
    IEnumerable<Oferta> resultado = ofertas;
    if (!string.IsNullOrWhiteSpace(busca)) resultado = resultado.Where(o => $"{o.Nome} {o.Titulo} {o.Descricao}".Contains(busca, StringComparison.OrdinalIgnoreCase));
    if (!string.IsNullOrWhiteSpace(categoria)) resultado = resultado.Where(o => o.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase));
    resultado = ordem?.ToLowerInvariant() switch { "avaliacao" => resultado.OrderByDescending(o => o.Nota), "preco" => resultado.OrderBy(o => o.Preco), _ => resultado.OrderBy(o => o.DistanciaKm) };
    return Results.Ok(resultado);
});
app.MapGet("/api/ofertas/{id:int}", (int id) => ofertas.FirstOrDefault(o => o.Id == id) is { } oferta ? Results.Ok(oferta) : Results.NotFound());
app.MapPost("/api/ofertas", (NovaOferta input) =>
{
    if (string.IsNullOrWhiteSpace(input.Nome) || string.IsNullOrWhiteSpace(input.Titulo) || string.IsNullOrWhiteSpace(input.Descricao) || input.Preco <= 0 || !categorias.Contains(input.Categoria))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["oferta"] = ["Informe nome, título, descrição, categoria válida e preço maior que zero."] });
    var oferta = new Oferta(ofertas.Max(o => o.Id) + 1, input.Nome.Trim(), input.Titulo.Trim(), input.Categoria, input.Preco, input.Unidade, 5, 0, 0, input.Descricao.Trim());
    ofertas.Add(oferta);
    return Results.Created($"/api/ofertas/{oferta.Id}", oferta);
});
app.Run();

record Oferta(int Id, string Nome, string Titulo, string Categoria, decimal Preco, string Unidade, double Nota, int Avaliacoes, double DistanciaKm, string Descricao);
record NovaOferta(string Nome, string Titulo, string Categoria, decimal Preco, string Unidade, string Descricao);
