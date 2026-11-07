using Gera.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gera.Infrastructure.Data.Configurations;

public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("Categorias");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Descricao)
            .HasMaxLength(300);

        builder.Property(c => c.Icone)
            .HasMaxLength(50);

        builder.HasOne(c => c.CategoriaPai)
            .WithMany(c => c.Subcategorias)
            .HasForeignKey(c => c.CategoriaPaiId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed das 8 Categorias Principais e Subcategorias
        builder.HasData(
            // Categorias Principais (1..8)
            new Categoria { Id = 1, Nome = "Construção", Descricao = "Obras, reformas, alvenaria, pisos e acabamentos", Icone = "Hammer", Ordem = 1, Ativo = true },
            new Categoria { Id = 2, Nome = "Elétrica", Descricao = "Instalações elétricas, fiação, quadros e iluminação", Icone = "Zap", Ordem = 2, Ativo = true },
            new Categoria { Id = 3, Nome = "Hidráulica", Descricao = "Encanamentos, vazamentos, pias, torneiras e caixas d'água", Icone = "Droplet", Ordem = 3, Ativo = true },
            new Categoria { Id = 4, Nome = "Pintura", Descricao = "Pintura residencial, predial, texturas e vernizes", Icone = "Paintbrush", Ordem = 4, Ativo = true },
            new Categoria { Id = 5, Nome = "Limpeza", Descricao = "Faxinas, pós-obra, estofados e higienização geral", Icone = "Sparkles", Ordem = 5, Ativo = true },
            new Categoria { Id = 6, Nome = "Fretes e Mudanças", Descricao = "Transporte de cargas, mudanças e carretos", Icone = "Truck", Ordem = 6, Ativo = true },
            new Categoria { Id = 7, Nome = "Jardinagem", Descricao = "Poda, corte de grama, paisagismo e limpeza de terreno", Icone = "Trees", Ordem = 7, Ativo = true },
            new Categoria { Id = 8, Nome = "Reparos Domésticos", Descricao = "Marido de aluguel, montagem de móveis e pequenos consertos", Icone = "Wrench", Ordem = 8, Ativo = true },

            // Subcategorias Construção (101..106)
            new Categoria { Id = 101, CategoriaPaiId = 1, Nome = "Alvenaria", Descricao = "Paredes, muros, vigas e fundações", Icone = "BrickWall", Ordem = 1, Ativo = true },
            new Categoria { Id = 102, CategoriaPaiId = 1, Nome = "Acabamento", Descricao = "Gesso, molduras, rodapés e detalhes", Icone = "Layers", Ordem = 2, Ativo = true },
            new Categoria { Id = 103, CategoriaPaiId = 1, Nome = "Piso e Revestimento", Descricao = "Porcelanato, cerâmica, azulejos e pastilhas", Icone = "Grid", Ordem = 3, Ativo = true },
            new Categoria { Id = 104, CategoriaPaiId = 1, Nome = "Telhado e Calhas", Descricao = "Construção e conserto de telhados e rufos", Icone = "Home", Ordem = 4, Ativo = true },
            new Categoria { Id = 105, CategoriaPaiId = 1, Nome = "Reboco e Laje", Descricao = "Chapisco, emboço, reboco e concretagem", Icone = "HardHat", Ordem = 5, Ativo = true },
            new Categoria { Id = 106, CategoriaPaiId = 1, Nome = "Estrutura e Fundação", Descricao = "Sapatas, colunas, brocas e baldrames", Icone = "ShieldCheck", Ordem = 6, Ativo = true },

            // Subcategorias Elétrica (201..206)
            new Categoria { Id = 201, CategoriaPaiId = 2, Nome = "Instalações Residenciais", Descricao = "Tomadas, interruptores e chuveiros", Icone = "Plug", Ordem = 1, Ativo = true },
            new Categoria { Id = 202, CategoriaPaiId = 2, Nome = "Troca de Fiação", Descricao = "Substituição completa ou parcial de condutores", Icone = "Cable", Ordem = 2, Ativo = true },
            new Categoria { Id = 203, CategoriaPaiId = 2, Nome = "Quadros de Luz e Disjuntores", Descricao = "Montagem, dimensionamento e substituição", Icone = "Cpu", Ordem = 3, Ativo = true },
            new Categoria { Id = 204, CategoriaPaiId = 2, Nome = "Padrão de Entrada", Descricao = "Instalação e adequação de padrão de energia", Icone = "ShieldAlert", Ordem = 4, Ativo = true },
            new Categoria { Id = 205, CategoriaPaiId = 2, Nome = "Iluminação e Lustres", Descricao = "Fitas LED, pendentes, spots e lustres", Icone = "Sun", Ordem = 5, Ativo = true },
            new Categoria { Id = 206, CategoriaPaiId = 2, Nome = "Manutenção e Reparos Elétricos", Descricao = "Diagnóstico de curto-circuito e panes", Icone = "Activity", Ordem = 6, Ativo = true },

            // Subcategorias Hidráulica (301..306)
            new Categoria { Id = 301, CategoriaPaiId = 3, Nome = "Desentupimento", Descricao = "Pias, ralos, vasos sanitários e caixas de gordura", Icone = "Disc", Ordem = 1, Ativo = true },
            new Categoria { Id = 302, CategoriaPaiId = 3, Nome = "Encanamento Geral", Descricao = "Tubulações de água fria, quente e esgoto", Icone = "GitCommit", Ordem = 2, Ativo = true },
            new Categoria { Id = 303, CategoriaPaiId = 3, Nome = "Caça-Vazamento", Descricao = "Detecção não destrutiva de infiltrações", Icone = "Search", Ordem = 3, Ativo = true },
            new Categoria { Id = 304, CategoriaPaiId = 3, Nome = "Instalação de Louças e Metais", Descricao = "Torneiras, registros, cubas e vasos", Icone = "CircleDot", Ordem = 4, Ativo = true },
            new Categoria { Id = 305, CategoriaPaiId = 3, Nome = "Caixa d'Água e Bombas", Descricao = "Limpeza, instalação e pressurizadores", Icone = "Archive", Ordem = 5, Ativo = true },
            new Categoria { Id = 306, CategoriaPaiId = 3, Nome = "Aquecedores", Descricao = "Aquecedores a gás, elétricos e solares", Icone = "Flame", Ordem = 6, Ativo = true },

            // Subcategorias Pintura (401..405)
            new Categoria { Id = 401, CategoriaPaiId = 4, Nome = "Pintura Residencial Interna", Descricao = "Tetos, paredes, portas e janelas", Icone = "Paintbrush", Ordem = 1, Ativo = true },
            new Categoria { Id = 402, CategoriaPaiId = 4, Nome = "Pintura Externa e Fachadas", Descricao = "Proteção e acabamento externo resistente", Icone = "Building", Ordem = 2, Ativo = true },
            new Categoria { Id = 403, CategoriaPaiId = 4, Nome = "Texturas e Efeitos Decorativos", Descricao = "Cimento queimado, marmorato e grafiato", Icone = "Feather", Ordem = 3, Ativo = true },
            new Categoria { Id = 404, CategoriaPaiId = 4, Nome = "Envernizamento de Madeiras", Descricao = "Decks, pergolados, portas e móveis", Icone = "Box", Ordem = 4, Ativo = true },
            new Categoria { Id = 405, CategoriaPaiId = 4, Nome = "Pintura de Portões e Grades", Descricao = "Proteção antiferrugem e esmalte sintético", Icone = "Shield", Ordem = 5, Ativo = true },

            // Subcategorias Limpeza (501..505)
            new Categoria { Id = 501, CategoriaPaiId = 5, Nome = "Faxina Diária / Residencial", Descricao = "Limpeza geral e manutenção do lar", Icone = "CheckCircle", Ordem = 1, Ativo = true },
            new Categoria { Id = 502, CategoriaPaiId = 5, Nome = "Limpeza Pós-Obra", Descricao = "Remoção de resíduos pesados e tintas", Icone = "Trash2", Ordem = 2, Ativo = true },
            new Categoria { Id = 503, CategoriaPaiId = 5, Nome = "Limpeza de Estofados", Descricao = "Sofás, colchões, tapetes e cortinas", Icone = "Armchair", Ordem = 3, Ativo = true },
            new Categoria { Id = 504, CategoriaPaiId = 5, Nome = "Limpeza Comercial", Descricao = "Lojas, consultórios e escritórios", Icone = "Briefcase", Ordem = 4, Ativo = true },
            new Categoria { Id = 505, CategoriaPaiId = 5, Nome = "Limpeza de Vidros e Janelas", Descricao = "Fachadas envidraçadas e sacadas", Icone = "Maximize2", Ordem = 5, Ativo = true },

            // Subcategorias Fretes e Mudanças (601..605)
            new Categoria { Id = 601, CategoriaPaiId = 6, Nome = "Pequenos Fretes", Descricao = "Carretos e transporte de pequenos volumes", Icone = "Package", Ordem = 1, Ativo = true },
            new Categoria { Id = 602, CategoriaPaiId = 6, Nome = "Mudança Residencial Completa", Descricao = "Embalagem, transporte e descarregamento", Icone = "Truck", Ordem = 2, Ativo = true },
            new Categoria { Id = 603, CategoriaPaiId = 6, Nome = "Transporte de Móveis e Eletros", Descricao = "Geladeiras, fogões, sofás e camas", Icone = "Tv", Ordem = 3, Ativo = true },
            new Categoria { Id = 604, CategoriaPaiId = 6, Nome = "Coletas e Entregas Expressas", Descricao = "Entregas rápidas no mesmo dia", Icone = "Clock", Ordem = 4, Ativo = true },
            new Categoria { Id = 605, CategoriaPaiId = 6, Nome = "Içamento de Cargas", Descricao = "Subida de peças grandes por sacadas", Icone = "ArrowUpCircle", Ordem = 5, Ativo = true },

            // Subcategorias Jardinagem (701..705)
            new Categoria { Id = 701, CategoriaPaiId = 7, Nome = "Corte de Grama e Roçagem", Descricao = "Aparação de gramados e áreas verdes", Icone = "Scissors", Ordem = 1, Ativo = true },
            new Categoria { Id = 702, CategoriaPaiId = 7, Nome = "Poda de Árvores e Cercas Vivas", Descricao = "Poda ornamental e de segurança", Icone = "Crop", Ordem = 2, Ativo = true },
            new Categoria { Id = 703, CategoriaPaiId = 7, Nome = "Paisagismo e Plantio", Descricao = "Criação de canteiros, adubação e mudas", Icone = "Heart", Ordem = 3, Ativo = true },
            new Categoria { Id = 704, CategoriaPaiId = 7, Nome = "Limpeza de Terrenos", Descricao = "Retirada de mato alto e entulho vegetal", Icone = "Slash", Ordem = 4, Ativo = true },
            new Categoria { Id = 705, CategoriaPaiId = 7, Nome = "Manutenção Periódica de Jardim", Descricao = "Contratos semanais ou mensais", Icone = "Calendar", Ordem = 5, Ativo = true },

            // Subcategorias Reparos Domésticos (801..805)
            new Categoria { Id = 801, CategoriaPaiId = 8, Nome = "Marido de Aluguel", Descricao = "Serviços gerais e reparos rápidos para o lar", Icone = "Wrench", Ordem = 1, Ativo = true },
            new Categoria { Id = 802, CategoriaPaiId = 8, Nome = "Montagem de Móveis", Descricao = "Montagem e desmontagem de armários, mesas e racks", Icone = "Tool", Ordem = 2, Ativo = true },
            new Categoria { Id = 803, CategoriaPaiId = 8, Nome = "Instalação de Cortinas e Quadros", Descricao = "Varões, persianas, espelhos e suportes de TV", Icone = "Layout", Ordem = 3, Ativo = true },
            new Categoria { Id = 804, CategoriaPaiId = 8, Nome = "Fechaduras e Portas", Descricao = "Troca de miolos, trincos, ajustes e dobradiças", Icone = "Key", Ordem = 4, Ativo = true },
            new Categoria { Id = 805, CategoriaPaiId = 8, Nome = "Pequenos Reparos em Geral", Descricao = "Silicone em box, troca de maçanetas e vedação", Icone = "CheckSquare", Ordem = 5, Ativo = true }
        );
    }
}
