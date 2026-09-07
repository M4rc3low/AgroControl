# ADR-004 — Bloqueio de módulos no backend

**Status:** Aceito

## Contexto

O produto exibirá módulos ativos, em breve e bloqueados por plano.

## Decisão

A interface pode ocultar/desabilitar ações, mas a autorização definitiva ocorre na API.

## Regra

Uma organização sem entitlement para um módulo protegido recebe `403 Forbidden` ao tentar chamar diretamente seu endpoint.

## Consequência

O controle de plano se torna parte da segurança e das regras do produto, não apenas uma decisão visual do frontend.
