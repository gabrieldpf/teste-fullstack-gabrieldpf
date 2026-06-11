
-- Seed PostgreSQL para o teste
create extension if not exists "uuid-ossp";

drop table if exists "public"."veiculo_cliente_historico" cascade;
drop table if exists "public"."fatura_veiculo" cascade;
drop table if exists "public"."fatura" cascade;
drop table if exists "public"."veiculo" cascade;
drop table if exists "public"."cliente" cascade;

create table "public"."cliente"(
  id uuid primary key default uuid_generate_v4(),
  nome text not null,
  telefone varchar(20),
  endereco varchar(400),
  mensalista boolean not null default false,
  valor_mensalidade numeric(12,2),
  data_inclusao timestamp not null default now()
);

create unique index ux_cliente_nome_telefone on "public"."cliente"(nome, telefone);

create table "public"."veiculo"(
  id uuid primary key default uuid_generate_v4(),
  placa varchar(8) not null unique,
  modelo varchar(120),
  ano int,
  data_inclusao timestamp not null default now(),
  cliente_id uuid not null references "public"."cliente"(id)
);

create table "public"."fatura"(
  id uuid primary key default uuid_generate_v4(),
  competencia varchar(7) not null, -- yyyy-MM
  cliente_id uuid not null references "public"."cliente"(id),
  valor numeric(12,2) not null default 0,
  criada_em timestamp not null default now(),
  observacao text
);

create unique index ux_fatura_cliente_competencia on "public"."fatura"(cliente_id, competencia);

create table "public"."fatura_veiculo"(
  fatura_id uuid not null references "public"."fatura"(id) on delete cascade,
  veiculo_id uuid not null references "public"."veiculo"(id),
  primary key (fatura_id, veiculo_id)
);

create table "public"."veiculo_cliente_historico"(
  id uuid primary key default uuid_generate_v4(),
  veiculo_id uuid not null references "public"."veiculo"(id) on delete cascade,
  cliente_id uuid not null references "public"."cliente"(id),
  data_inicio timestamp not null,
  data_fim timestamp
);

create index ix_veiculo_historico_veiculo on "public"."veiculo_cliente_historico"(veiculo_id, data_inicio);

-- Clientes
insert into "public"."cliente"(id, nome, telefone, endereco, mensalista, valor_mensalidade) values
  ('11111111-1111-1111-1111-111111111111','João Souza','31999990001','Rua A, 123',true,189.90),
  ('22222222-2222-2222-2222-222222222222','Maria Lima','31988880002','Av. B, 456',false,null),
  ('33333333-3333-3333-3333-333333333333','Carlos Silva','31977770003','Rua C, 789',true,159.90),
  ('44444444-4444-4444-4444-444444444444','Ana Paula','31966660004','Av. D, 101',false,null),
  ('55555555-5555-5555-5555-555555555555','Beatriz Melo','31955550005','Rua E, 202',true,209.90);

-- Veículos (inclui caso de troca no meio do mês 2025-08)
insert into "public"."veiculo"(id, placa, modelo, ano, cliente_id, data_inclusao) values
  ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1','BRA1A23','Gol',2019,'11111111-1111-1111-1111-111111111111','2025-07-10'),
  ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2','RCH2B45','Onix',2020,'22222222-2222-2222-2222-222222222222','2025-07-15'),
  ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3','ABC1D23','HB20',2018,'22222222-2222-2222-2222-222222222222','2025-08-18'),
  ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4','QWE1Z89','Argo',2021,'33333333-3333-3333-3333-333333333333','2025-07-20'),
  ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5','JKL2M34','Fox',2017,'33333333-3333-3333-3333-333333333333','2025-08-05'),
  ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa6','ZTB3N56','Civic',2022,'55555555-5555-5555-5555-555555555555','2025-07-01'),
  ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7','HGF4P77','Corolla',2022,'55555555-5555-5555-5555-555555555555','2025-08-20'),
  ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa8','AAA1A11','Uno',2015,'22222222-2222-2222-2222-222222222222','2025-07-01');

-- Histórico de propriedade dos veículos
insert into "public"."veiculo_cliente_historico"(id, veiculo_id, cliente_id, data_inicio, data_fim) values
  ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1','11111111-1111-1111-1111-111111111111','2025-07-10',null),
  ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2','22222222-2222-2222-2222-222222222222','2025-07-15',null),
  ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3','11111111-1111-1111-1111-111111111111','2025-08-01','2025-08-17 23:59:59'),
  ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb4','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3','22222222-2222-2222-2222-222222222222','2025-08-18',null),
  ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb5','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4','33333333-3333-3333-3333-333333333333','2025-07-20',null),
  ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb6','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5','33333333-3333-3333-3333-333333333333','2025-08-05',null),
  ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb7','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa6','55555555-5555-5555-5555-555555555555','2025-07-01',null),
  ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb8','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7','55555555-5555-5555-5555-555555555555','2025-08-20',null),
  ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb9','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa8','22222222-2222-2222-2222-222222222222','2025-07-01',null);
