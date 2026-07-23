# Refinamento futuro — Totem de Autoatendimento e Acolhimento

Status: **Backlog — implementação posterior**  
Produto: Ello Clinic  
Nome provisório: **Ello Check-in**

## 1. Visão do produto

Disponibilizar uma experiência touchscreen na recepção para o paciente realizar check-in, atualizar dados básicos e, quando for um primeiro atendimento, preencher um acolhimento guiado. As respostas poderão sugerir especialidades para avaliação, sempre com revisão humana e sem produzir diagnóstico clínico automático.

## 2. Objetivos

- Reduzir filas e tarefas repetitivas da recepção.
- Atualizar o status da agenda quando o paciente chegar.
- Melhorar a qualidade do cadastro inicial.
- Organizar o motivo da procura antes do acolhimento profissional.
- Direcionar novos pacientes para especialidades potencialmente adequadas.
- Identificar respostas que exijam atenção imediata da equipe.
- Criar uma experiência acessível, rápida e coerente com a marca Ello Clinic.

## 3. Princípios obrigatórios

- O sistema não apresenta diagnóstico, prescrição ou certeza clínica.
- Toda sugestão de especialidade é orientativa e revisada por profissional autorizado.
- O totem nunca exibe a agenda completa da clínica.
- CPF ou e-mail isoladamente não autorizam acesso ao agendamento.
- São coletados somente dados necessários para a finalidade informada.
- A sessão é descartável e limpa automaticamente.
- Dados clínicos reais só poderão ser utilizados após revisão jurídica, LGPD e clínica.

## 4. Personas

### Paciente agendado

Deseja informar que chegou sem aguardar atendimento da recepção.

### Novo paciente

Deseja registrar dados básicos e informar, em linguagem simples, o motivo da procura.

### Responsável legal

Realiza o fluxo em nome de criança, adolescente ou pessoa sob sua responsabilidade.

### Recepcionista

Acompanha check-ins, auxilia pacientes e revisa cadastros incompletos.

### Profissional clínico

Recebe o resumo do acolhimento, revisa alertas e valida o direcionamento sugerido.

### Administrador da clínica

Configura o dispositivo, textos de consentimento, especialidades e regras permitidas.

## 5. Fluxo inicial do touchscreen

A tela inicial deverá apresentar três ações:

1. **Cheguei para meu atendimento**
2. **É meu primeiro atendimento**
3. **Tenho um código ou QR Code**

Também deverá oferecer:

- botão “Preciso de ajuda”;
- seleção de idioma, futuramente;
- recursos de acessibilidade;
- aviso de privacidade resumido;
- indicação de que a sessão será apagada ao final.

## 6. Fluxo de check-in

1. Paciente informa código de agendamento ou lê QR Code.
2. Como alternativa, informa CPF/e-mail e um segundo fator.
3. Sistema mostra somente atendimentos elegíveis do próprio paciente naquele dia.
4. Paciente seleciona o atendimento.
5. Confirma a ação **“Já cheguei”**.
6. Agendamento recebe o status `Arrived`.
7. Recepção e profissional recebem atualização.
8. Totem exibe orientação de espera sem revelar dados de terceiros.
9. Sessão e dados visíveis são apagados.

### Regras

- Não permitir check-in muito antes ou depois da janela configurada.
- Check-in duplicado deve retornar mensagem amigável.
- Atendimento cancelado ou inexistente não deve revelar detalhes.
- Falhas de identidade devem produzir resposta genérica.
- Após tentativas repetidas, aplicar bloqueio temporário.

## 7. Fluxo do primeiro atendimento

### Cadastro breve

- Nome e nome social.
- Data de nascimento.
- E-mail ou telefone.
- CPF conforme necessidade e base legal definida.
- Nome e contato do responsável legal.
- Preferência de unidade, horário e forma de contato.
- Aceite versionado do aviso de privacidade.

### Acolhimento guiado

O paciente seleciona cartões de necessidade, por exemplo:

- aspectos emocionais;
- dor, movimento e reabilitação;
- alimentação;
- fala, linguagem ou comunicação;
- aprendizagem e desenvolvimento;
- atenção e comportamento;
- pós-operatório;
- outro motivo.

As perguntas seguintes são adaptadas conforme as respostas:

- início e duração;
- intensidade percebida;
- impacto em sono, estudo, trabalho e rotina;
- acompanhamento e diagnóstico anteriores;
- uso de medicação, sem sugerir alteração;
- preferências e objetivos do acompanhamento;
- sinais de alerta configurados pela equipe clínica.

## 8. Resultado do acolhimento

O sistema poderá gerar:

- resumo estruturado das respostas;
- até três especialidades potencialmente adequadas;
- justificativas simples para cada sugestão;
- nível de prioridade administrativa;
- marcadores que exigem revisão humana;
- perguntas que o profissional pode aprofundar;
- status `AwaitingClinicalReview`.

Mensagem obrigatória:

> Esta orientação não é um diagnóstico. O direcionamento será revisado pela equipe da clínica antes do agendamento.

## 9. Segurança clínica

Respostas críticas deverão interromper o fluxo comum e orientar contato imediato com a equipe. Exemplos a serem definidos por protocolo profissional:

- risco de suicídio ou automutilação;
- falta de ar ou perda de consciência;
- sinais compatíveis com emergência neurológica ou cardíaca;
- dor súbita ou intensa;
- violência ou abuso;
- emergência envolvendo criança;
- deterioração rápida.

O totem não deverá classificar, diagnosticar ou recomendar tratamento nesses casos. Ele deverá alertar discretamente a equipe e mostrar instrução clara para procurar ajuda presencial.

## 10. Experiência touchscreen

- PWA em modo quiosque e tela cheia.
- Botões com área mínima de toque entre 48 e 56 pixels.
- Uma decisão principal por tela.
- Teclado numérico próprio para códigos e documentos.
- Barra de progresso.
- Alto contraste e fonte ajustável.
- Botões consistentes de voltar, cancelar e pedir ajuda.
- Linguagem simples, sem jargões clínicos.
- Suporte a responsável legal.
- Reinício após 30 a 60 segundos de inatividade.
- Confirmação antes de abandonar formulário parcialmente preenchido.
- Nenhum autocomplete, histórico ou salvamento de senha.

## 11. Segurança e privacidade

- Sessão exclusiva de quiosque, diferente do login de colaboradores.
- Token curto, restrito ao dispositivo e à finalidade.
- Nenhum JWT persistido em `localStorage`.
- Limpeza de memória, formulários e cache após cada atendimento.
- Rate limit por dispositivo, IP e identificador pesquisado.
- Proteção contra enumeração de CPF, e-mail e agendamentos.
- Criptografia em trânsito e em repouso.
- Auditoria de check-in, aceite, alteração cadastral e revisão clínica.
- Filtro físico de privacidade e posicionamento seguro do monitor.
- Bloqueio do navegador, sistema operacional e atalhos administrativos.
- Rede isolada e dispositivo gerenciado.
- Política de retenção para formulários abandonados.
- Avaliação de impacto à proteção de dados antes da produção.

## 12. Permissões

### Totem

- Consultar atendimento por token temporário.
- Realizar check-in.
- Criar pré-cadastro.
- Enviar acolhimento.
- Não consultar prontuário, financeiro, relatórios ou outros pacientes.

### Recepção

- Visualizar fila de chegada.
- Revisar pré-cadastros administrativos.
- Solicitar correção ou finalizar cadastro.
- Não validar conclusão clínica.

### Profissional autorizado

- Revisar acolhimento.
- Validar ou alterar especialidades sugeridas.
- Registrar justificativa.
- Converter pré-cadastro em fluxo de atendimento.

## 13. Modelo de dados proposto

### `KioskDevice`

- `Id`, `TenantId`, `UnitId`, `Name`
- `DeviceKeyHash`, `Active`, `LastSeenAt`
- `SessionTimeoutSeconds`, `CheckInWindowMinutes`

### `KioskSession`

- `Id`, `TenantId`, `DeviceId`
- `Purpose`, `StartedAt`, `ExpiresAt`, `CompletedAt`
- `Status`, `Attempts`, `IpAddress`

### `PatientCheckIn`

- `Id`, `TenantId`, `AppointmentId`, `PatientId`
- `DeviceId`, `CheckedInAt`, `PreviousStatus`

### `PatientIntake`

- `Id`, `TenantId`, `PatientId?`, `PreRegistrationId?`
- `QuestionnaireVersion`, `AnswersJson`
- `Status`, `SubmittedAt`, `ReviewedAt`, `ReviewedByUserId`

### `TriageRecommendation`

- `Id`, `TenantId`, `IntakeId`, `SpecialtyId`
- `Score`, `Reason`, `Priority`, `RequiresHumanReview`
- `Approved`, `ApprovedByUserId`, `ApprovedAt`

### `ConsentRecord`

- `Id`, `TenantId`, `PatientId?`, `IntakeId?`
- `DocumentVersion`, `AcceptedAt`, `DeviceId`

## 14. Endpoints propostos

- `POST /api/kiosk/session`
- `POST /api/kiosk/identify`
- `GET /api/kiosk/appointments/{accessToken}`
- `POST /api/kiosk/appointments/{id}/check-in`
- `POST /api/kiosk/pre-registrations`
- `GET /api/kiosk/questionnaires/current`
- `POST /api/kiosk/intakes`
- `POST /api/kiosk/sessions/{id}/finish`
- `GET /api/reception/check-ins`
- `GET /api/clinical/intakes/pending`
- `POST /api/clinical/intakes/{id}/review`

Todos os endpoints do totem deverão possuir autenticação própria, escopo mínimo, rate limit e auditoria.

## 15. Estratégia de recomendação

### Primeira versão

Motor determinístico de regras versionadas, criado e aprovado por profissionais. Cada resposta soma indicadores para especialidades, sem produzir diagnóstico.

### Evolução futura

Modelos estatísticos ou IA poderão resumir respostas e apoiar o direcionamento somente após base clínica validada, avaliação de risco, explicabilidade e revisão humana obrigatória.

## 16. Fases de implementação

### Fase 1 — Check-in

- Modo quiosque.
- Código/QR Code.
- Consulta limitada da agenda do dia.
- Botão “Cheguei”.
- Fila da recepção.
- Sessão descartável e auditoria.

### Fase 2 — Pré-cadastro

- Cadastro breve.
- Responsável legal.
- Aviso de privacidade e consentimento versionado.
- Revisão pela recepção.

### Fase 3 — Acolhimento orientativo

- Questionário adaptativo.
- Regras versionadas.
- Sinais de alerta.
- Sugestão de especialidades.
- Revisão profissional obrigatória.

### Fase 4 — Evoluções comerciais

- Pesquisa de satisfação.
- Assinatura de documentos.
- Múltiplos dispositivos por unidade.
- Personalização visual por clínica.
- Indicadores de fila, chegada e conversão.

## 17. Fora do escopo inicial

- Diagnóstico clínico automático.
- Prescrição ou recomendação de medicamento.
- Alteração de tratamento.
- Atendimento de emergência automatizado.
- Reconhecimento facial ou biometria.
- Pagamento no totem.
- Impressão de documentos.
- Integração com hardware específico além do touchscreen e câmera para QR Code.

## 18. Critérios de aceite da primeira fase

- Paciente realiza check-in em até 60 segundos.
- Apenas atendimentos próprios e elegíveis são exibidos.
- Status `Arrived` aparece na agenda em até cinco segundos.
- Tentativas com identificadores inválidos não revelam existência de cadastro.
- Sessão é apagada após conclusão ou inatividade.
- Totem não acessa nenhum módulo administrativo ou clínico.
- Fluxo funciona em telas de 10 a 24 polegadas.
- Auditoria registra dispositivo, horário, ação e agendamento.
- Testes automatizados cobrem autenticação, isolamento entre clínicas, enumeração e expiração.

## 19. Dependências antes de iniciar

- Aprovação de orçamento e prioridade do roadmap.
- Validação do fluxo pela clínica piloto.
- Definição do hardware e modo quiosque.
- Revisão jurídica e elaboração do RIPD.
- Protocolos aprovados por profissionais das especialidades atendidas.
- Definição do segundo fator de identificação.
- Política de retenção e descarte.
- Ambiente de testes separado com dados fictícios.

## 20. Decisões pendentes

- Usar código de seis dígitos, QR Code ou ambos?
- Qual janela permite check-in antes e depois do horário?
- Quais especialidades entram no primeiro questionário?
- Quem pode revisar e aprovar o direcionamento?
- O pré-cadastro reserva horário ou apenas gera contato para a recepção?
- Qual hardware e tamanho de tela serão usados?
- A clínica deseja acessibilidade por áudio?
- Haverá impressão de senha ou apenas orientação visual?

## 21. Condição para sair do backlog

Este refinamento deve permanecer sem implementação até que a fase atual de homologação do Ello Clinic seja concluída, os fluxos principais estejam estáveis e a clínica piloto aprove formalmente o escopo da Fase 1.
