# -*- coding: utf-8 -*-
"""One-off patch: replace UI emojis with app-icon markup (UTF-8 safe)."""
from pathlib import Path

ROOT = Path(__file__).resolve().parent


def rw(rel: str, fn):
    p = ROOT / rel
    t = p.read_text(encoding="utf-8")
    t2 = fn(t)
    if t2 != t:
        p.write_text(t2, encoding="utf-8")
        print("updated", rel)


def main():
    # --- icon: gear ---
    gear_block = """  <svg *ngSwitchCase="'gear'" [attr.width]="dim" [attr.height]="dim" viewBox="0 0 24 24" fill="currentColor"
       [attr.aria-hidden]="decorative" [attr.role]="label ? 'img' : null" [attr.aria-label]="label || null">
    <path d="M19.14 12.94c.04-.31.06-.63.06-.94 0-.31-.02-.63-.06-.94l2.03-1.58c.18-.14.23-.41.12-.61l-1.92-3.32c-.12-.22-.37-.29-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54c-.04-.24-.24-.41-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.05.31-.07.63-.07.94s.02.63.06.94l-2.03 1.58c-.18.14-.23.41-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z"/>
  </svg>
"""
    rw(
        "src/app/components/icon/icon.component.html",
        lambda t: t.replace(
            "  <svg *ngSwitchDefault",
            gear_block + "  <svg *ngSwitchDefault",
            1,
        ),
    )

    # --- higher-or-lower ---
    def hol_html(t: str) -> str:
        t = t.replace(
            '<span class="cat-icon">\U0001f3ac</span>',
            '<span class="cat-icon"><app-icon name="film" [size]="28"></app-icon></span>',
        )
        t = t.replace(
            '<span class="cat-icon">\U0001f3ad</span>',
            '<span class="cat-icon"><app-icon name="theater" [size]="28"></app-icon></span>',
        )
        t = t.replace(
            '<span class="cat-icon">\U0001f31f</span>',
            '<span class="cat-icon"><app-icon name="star" [size]="28"></app-icon></span>',
        )
        t = t.replace(
            '<span class="cat-icon">\u2696\ufe0f</span>',
            '<span class="cat-icon"><app-icon name="scale" [size]="28"></app-icon></span>',
        )
        t = t.replace(
            '<span class="cat-icon">\U0001f525</span>',
            '<span class="cat-icon"><app-icon name="flame" [size]="28"></app-icon></span>',
        )
        t = t.replace(
            """    <button type="button" class="history-btn" (click)="openHistory()">
      <span>\U0001f4dc</span> Histórico
    </button>""",
            """    <button type="button" class="history-btn" (click)="openHistory()">
      <span class="history-btn__ico"><app-icon name="scroll" [size]="18"></app-icon></span> Histórico
    </button>""",
        )
        t = t.replace(
            """    <button class="history-btn leaderboard-btn" (click)="openLeaderboard()">
      <span>\U0001f3c6</span> Leaderboard
    </button>""",
            """    <button class="history-btn leaderboard-btn" (click)="openLeaderboard()">
      <span class="history-btn__ico"><app-icon name="trophy" [size]="18"></app-icon></span> Leaderboard
    </button>""",
        )
        t = t.replace(
            "              Escolhido \u2713\n",
            '              Escolhido <app-icon name="check" [size]="14" class="chosen-check-ico"></app-icon>\n',
        )
        t = t.replace(
            "              \u2b50 {{ leftActorPopularity | number:'1.1-1' }}\n",
            '              <span class="pop-badge-inner"><app-icon name="star" [size]="14"></app-icon> {{ leftActorPopularity | number:\'1.1-1\' }}</span>\n',
        )
        t = t.replace(
            "              \u2b50 {{ rightActorPopularity | number:'1.1-1' }}\n",
            '              <span class="pop-badge-inner"><app-icon name="star" [size]="14"></app-icon> {{ rightActorPopularity | number:\'1.1-1\' }}</span>\n',
        )
        t = t.replace(
            """    <div class="notif notif-correct" *ngIf="notifier === 'correct'">
      <span class="notif-icon">\u2713</span> Correto! A avan\u00e7ar\u2026
    </div>""",
            """    <div class="notif notif-correct" *ngIf="notifier === 'correct'">
      <span class="notif-icon"><app-icon name="check" [size]="18" [decorative]="true"></app-icon></span> Correto! A avan\u00e7ar\u2026
    </div>""",
        )
        t = t.replace(
            """    <div class="notif notif-wrong" *ngIf="notifier === 'wrong'">
      <span class="notif-icon">\u2717</span> Errado \u2014 Game over.
    </div>""",
            """    <div class="notif notif-wrong" *ngIf="notifier === 'wrong'">
      <span class="notif-icon"><app-icon name="close" [size]="18" [decorative]="true"></app-icon></span> Errado \u2014 Game over.
    </div>""",
        )
        # wrong notif� (U+2717) or different - check file
        t = t.replace(
            '<div class="endgame-emoji">\U0001f3c6</div>',
            '<div class="endgame-emoji" aria-hidden="true"><app-icon name="trophy" [size]="56"></app-icon></div>',
        )
        t = t.replace(
            '          <span class="xp-icon">\u26a1</span>\n',
            '          <span class="xp-icon"><app-icon name="bolt" [size]="24"></app-icon></span>\n',
        )
        t = t.replace(
            '        <h2 class="history-title">\U0001f4dc Hist\u00f3rico</h2>',
            '        <h2 class="history-title"><span class="history-title__ico"><app-icon name="scroll" [size]="26"></app-icon></span> Hist\u00f3rico</h2>',
        )
        t = t.replace(
            '          <span class="stat-icon">\U0001f3c6</span>\n',
            '          <span class="stat-icon"><app-icon name="trophy" [size]="22"></app-icon></span>\n',
        )
        t = t.replace(
            '          <span class="stat-icon">\U0001f4ca</span>\n',
            '          <span class="stat-icon"><app-icon name="chart" [size]="22"></app-icon></span>\n',
        )
        t = t.replace(
            '          <span class="stat-icon">\U0001f3ae</span>\n',
            '          <span class="stat-icon"><app-icon name="gamepad" [size]="22"></app-icon></span>\n',
        )
        old_badge = """ <div class="hi-badge"
               [class.badge-actors]="h.category === 'Atores'"
               [class.badge-films]="h.category !== 'Atores'">
            {{ h.category === 'Atores' ? '\U0001f3ad Atores' : '\U0001f3ac Filmes' }}
          </div>"""
        new_badge = """          <div class="hi-badge"
               [class.badge-actors]="h.category === 'Atores'"
               [class.badge-films]="h.category !== 'Atores'">
            <ng-container *ngIf="h.category === 'Atores'; else hiBadgeFilmes">
              <app-icon name="theater" [size]="14" class="hi-badge__ico"></app-icon> Atores
            </ng-container>
            <ng-template #hiBadgeFilmes>
              <app-icon name="film" [size]="14" class="hi-badge__ico"></app-icon> Filmes
            </ng-template>
          </div>"""
        t = t.replace(old_badge, new_badge)
        return t

    rw("src/app/components/higher-or-lower/higher-or-lower.component.html", hol_html)

    def hol_css(t: str) -> str:
        if ".history-btn__ico" not in t:
            t = t.replace(
                ".cat-icon {\n  font-size: 28px;\n}",
                ".cat-icon {\n  display: inline-flex;\n  align-items: center;\n  justify-content: center;\n}\n\n.history-btn__ico {\n  display: inline-flex;\n  margin-right: 6px;\n  vertical-align: middle;\n}\n\n.hi-badge__ico {\n  display: inline-flex;\n  margin-right: 4px;\n  vertical-align: middle;\n}\n\n.history-title__ico {\n  display: inline-flex;\n  margin-right: 8px;\n  vertical-align: middle;\n}\n\n.chosen-check-ico {\n  display: inline-flex;\n  vertical-align: middle;\n  margin-left: 2px;\n}\n\n.pop-badge-inner {\n  display: inline-flex;\n  align-items: center;\n  gap: 4px;\n}",
            )
        # menu home cat-icon font-size override -> align icon
        t = t.replace(
            ".hol-page.hol-menu-home .cat-icon {\n  font-size: 24px;\n}",
            ".hol-page.hol-menu-home .cat-icon app-icon {\n  display: inline-flex;\n}\n",
        )
        t = t.replace(
            "  .cat-icon {\n    font-size: 24px;\n  }\n",
            "  .cat-icon app-icon {\n    display: inline-flex;\n  }\n",
        )
        t = t.replace(
            ".endgame-emoji {\n  font-size: 56px;\n  margin-bottom: 4px;\n}",
            ".endgame-emoji {\n  margin-bottom: 4px;\n  display: flex;\n  align-items: center;\n  justify-content: center;\n  color: #fbbf24;\n}",
        )
        t = t.replace(
            ".stat-icon {\n  font-size: 20px;\n}",
            ".stat-icon {\n  display: inline-flex;\n  align-items: center;\n  justify-content: center;\n}\n",
        )
        t = t.replace(
            ".notif-icon {\n  font-size: 18px;\n}",
            ".notif-icon {\n  display: inline-flex;\n  align-items: center;\n}\n",
        )
        return t

    rw("src/app/components/higher-or-lower/higher-or-lower.component.css", hol_css)

    # Fix not� vs ×
    p_hol = ROOT / "src/app/components/higher-or-lower/higher-or-lower.component.html"
    ht = p_hol.read_text(encoding="utf-8")
    if "\u2717" not in ht and "notif-wrong" in ht and "notif-icon" in ht:
        ht = ht.replace(
            '<span class="not�</span>',
            '<span class="notif-icon"><app-icon name="close" [size]="18" [decorative]="true"></app-icon></span>',
        )
        p_hol.write_text(ht, encoding="utf-8")
        print("patched hol notif-wrong alt char")

    # --- dashboard ---
    def dash_html(t: str) -> str:
        t = t.replace(
            "<h2>Recomendado para ti \U0001f3af</h2>",
            '<h2><span class="section-head-ico"><app-icon name="target" [size]="22"></app-icon></span> Recomendado para ti</h2>',
        )
        t = t.replace(
            "                \u2b50 {{ r.communityAverage }}/10\n",
 '                <span class="rec-rating-stars"><app-icon name="star" [size]="16"></app-icon> {{ r.communityAverage }}/10</span>\n',
        )
        t = t.replace(
            """                        aria-label="Recomenda\u00e7\u00e3o irrelevante">
                  \U0001f44e
                </button>""",
            """                        aria-label="Recomenda\u00e7\u00e3o irrelevante">
                  <app-icon name="thumbsDown" [size]="22"></app-icon>
                </button>""",
        )
        t = t.replace(
            """                        aria-label="Recomenda\u00e7\u00e3o relevante">
                  \U0001f44d
                </button>""",
            """                        aria-label="Recomenda\u00e7\u00e3o relevante">
                  <app-icon name="thumbsUp" [size]="22"></app-icon>
                </button>""",
        )
        t = t.replace(
            "<ng-container *ngIf=\"respostaCorretaVisivel === 'A'\">\u2705</ng-container>",
            '<ng-container *ngIf="respostaCorretaVisivel === \'A\'"><app-icon name="check" [size]="18"></app-icon></ng-container>',
        )
        t = t.replace(
            "<ng-container *ngIf=\"opcaoSelecionada === 'A' && respostaCorretaVisivel !== 'A'\">\u274c</ng-container>",
            '<ng-container *ngIf="opcaoSelecionada === \'A\' && respostaCorretaVisivel !== \'A\'"><app-icon name="close" [size]="18"></app-icon></ng-container>',
        )
        for L in ("B", "C"):
            t = t.replace(
                f"<ng-container *ngIf=\"respostaCorretaVisivel === '{L}'\">\u2705</ng-container>",
                f'<ng-container *ngIf="respostaCorretaVisivel === \'{L}\'"><app-icon name="check" [size]="18"></app-icon></ng-container>',
            )
            t = t.replace(
                f"<ng-container *ngIf=\"opcaoSelecionada === '{L}' && respostaCorretaVisivel !== '{L}'\">\u274c</ng-container>",
                f'<ng-container *ngIf="opcaoSelecionada === \'{L}\' && respostaCorretaVisivel !== \'{L}\'"><app-icon name="close" [size]="18"></app-icon></ng-container>',
            )
        t = t.replace(
            "{{ feedbackDesafio || (desafioDoDia.acertou ? 'J\u00e1 respondeste corretamente hoje! \U0001f389' : 'J\u00e1 respondeste a este desafio (Incorreto).') }}",
            "{{ feedbackDesafio || (desafioDoDia.acertou ? 'J\u00e1 respondeste corretamente hoje!' : 'J\u00e1 respondeste a este desafio (Incorreto).') }}",
        )
        t = t.replace(
            '          <span class="quiz-reward">\u2728 Recompensa: {{ desafioDoDia.xp }} XP</span>',
            '          <span class="quiz-reward"><app-icon name="sparkles" [size]="18" class="quiz-reward-ico"></app-icon> Recompensa: {{ desafioDoDia.xp }} XP</span>',
        )
        t = t.replace(
            '<span class="next-medal" *ngIf="medalProgress.nextMedalName === \'Todas conquistadas!\'">\U0001f389 Todas as medalhas conquistadas!</span>',
            '<span class="next-medal" *ngIf="medalProgress.nextMedalName === \'Todas conquistadas!\'"><app-icon name="party" [size]="16" class="inline-ico"></app-icon> Todas as medalhas conquistadas!</span>',
        )
        return t

    rw("src/app/components/dashboard/dashboard.component.html", dash_html)

    rw(
        "src/app/components/dashboard/dashboard.component.ts",
        lambda t: t.replace(
            "this.feedbackDesafio = `Correto! Ganhaste ${res.xpGanho || this.desafioDoDia.xp} XP! \U0001f389`;",
            "this.feedbackDesafio = `Correto! Ganhaste ${res.xpGanho || this.desafioDoDia.xp} XP!`;",
        ).replace(
            "this.feedbackDesafio = 'Incorreto! Tenta novamente amanh\u00e3. \U0001f3ac';",
            "this.feedbackDesafio = 'Incorreto! Tenta novamente amanh\u00e3.';",
        ),
    )

    # --- topbar-actions ---
    def topbar(t: str) -> str:
        old = """              <div class="notif-icon"
                   [ngClass]="{
                     'notif-icon--pink': n.tipo === 'comunidade',
                     'notif-icon--purple': n.tipo === 'medalha' || n.tipo === 'jogo',
                     'notif-icon--blue': n.tipo === 'filme',
                     'notif-icon--green': n.tipo === 'resumo',
                     'notif-icon--orange': n.tipo === 'plataforma'
                   }">
                {{
                  n.tipo === 'comunidade' ? '\U0001f465' :
                  n.tipo === 'medalha' ? '\U0001f3c5' :
                  n.tipo === 'filme' ? '\U0001f3ac' :
                  n.tipo === 'resumo' ? '\U0001f4ca' :
                  n.tipo === 'plataforma' ? '\U0001f4e2' :
                  '\U0001f3ae'
                }}
              </div>"""
        new = """              <div class="notif-icon"
                   [ngClass]="{
                     'notif-icon--pink': n.tipo === 'comunidade',
                     'notif-icon--purple': n.tipo === 'medalha' || n.tipo === 'jogo',
                     'notif-icon--blue': n.tipo === 'filme',
                     'notif-icon--green': n.tipo === 'resumo',
                     'notif-icon--orange': n.tipo === 'plataforma'
                   }">
 <ng-container [ngSwitch]="n.tipo">
                  <app-icon *ngSwitchCase="'comunidade'" name="users" [size]="22"></app-icon>
                  <app-icon *ngSwitchCase="'medalha'" name="medal" [size]="22"></app-icon>
                  <app-icon *ngSwitchCase="'filme'" name="film" [size]="22"></app-icon>
                  <app-icon *ngSwitchCase="'resumo'" name="chart" [size]="22"></app-icon>
                  <app-icon *ngSwitchCase="'plataforma'" name="megaphone" [size]="22"></app-icon>
                  <app-icon *ngSwitchCase="'jogo'" name="gamepad" [size]="22"></app-icon>
                  <app-icon *ngSwitchDefault name="megaphone" [size]="22"></app-icon>
                </ng-container>
              </div>"""
        t = t.replace(old, new)
        t = t.replace(
            """ <button type="button"
                          class="notification-mark-read"
                          (click)="marcarComoLida($event, m)"
                          aria-label="Marcar como vista">
                    \u2713
                  </button>""",
            """                  <button type="button"
                          class="notification-mark-read"
                          (click)="marcarComoLida($event, m)"
                          aria-label="Marcar como vista">
                    <app-icon name="check" [size]="18"></app-icon>
                  </button>""",
        )
        t = t.replace(
            """              <button type="button"
                      class="notif-check-btn"
                      *ngIf="!n.lida"
                      (click)="marcarNotifComoLida($event, n)"
                      aria-label="Marcar como lida">
                \u2713
              </button>""",
            """              <button type="button"
                      class="notif-check-btn"
                      *ngIf="!n.lida"
                      (click)="marcarNotifComoLida($event, n)"
                      aria-label="Marcar como lida">
                <app-icon name="check" [size]="18"></app-icon>
              </button>""",
        )
        t = t.replace(
            ' \U0001f3a5 {{ n.raw.corpo.filmeMaisVistoSemanaPlataforma.titulo }}',
            '                      <span class="resumo-film-ico"><app-icon name="video" [size]="14"></app-icon></span> {{ n.raw.corpo.filmeMaisVistoSemanaPlataforma.titulo }}',
        )
        t = t.replace(
            "<ng-container *ngSwitchCase=\"'pedido_aprovado'\">O teu pedido para a comunidade <strong>{{ n.raw.comunidadeNome }}</strong> foi aprovado \u2705</ng-container>",
            "<ng-container *ngSwitchCase=\"'pedido_aprovado'\"><span class=\"notif-inline-ico\"><app-icon name=\"check\" [size]=\"14\"></app-icon></span> O teu pedido para a comunidade <strong>{{ n.raw.comunidadeNome }}</strong> foi aprovado</ng-container>",
        )
        t = t.replace(
            "<ng-container *ngSwitchCase=\"'pedido_rejeitado'\">O teu pedido para a comunidade <strong>{{ n.raw.comunidadeNome }}</strong> foi rejeitado \u274c</ng-container>",
            "<ng-container *ngSwitchCase=\"'pedido_rejeitado'\"><span class=\"notif-inline-ico\"><app-icon name=\"close\" [size]=\"14\"></app-icon></span> O teu pedido para a comunidade <strong>{{ n.raw.comunidadeNome }}</strong> foi rejeitado</ng-container>",
        )
        return t

    rw("src/app/components/topbar-actions/topbar-actions.component.html", topbar)

    # --- login ---
    rw(
        "src/app/components/login/login.component.html",
        lambda t: t.replace(
            '<h3 style="color: #856404; margin-top: 0;">\u26a0\ufe0f Email n\u00e3o confirmado</h3>',
            '<h3 style="color: #856404; margin-top: 0; display:flex; align-items:center; gap:8px;"><app-icon name="warning" [size]="22"></app-icon> Email n\u00e3o confirmado</h3>',
        ),
    )

    # --- register ---
    def reg(t: str) -> str:
        t = t.replace(
            '<h3 style="color: #0c5460; margin-top: 0;">\U0001f4e7 Verifique o seu email!</h3>',
            '<h3 style="color: #0c5460; margin-top: 0; display:flex; align-items:center; gap:8px;"><app-icon name="mail" [size]="22"></app-icon> Verifique o seu email!</h3>',
        )
        t = t.replace(
            '<span *ngIf="!showPassword">\U0001f441</span>',
            '<app-icon *ngIf="!showPassword" name="eye" [size]="20"></app-icon>',
        )
        t = t.replace(
            '<span *ngIf="showPassword" class="eye-crossed">\U0001f441</span>',
            '<app-icon *ngIf="showPassword" name="eyeOff" [size]="20"></app-icon>',
        )
        t = t.replace(
            '<span *ngIf="!showConfirmPassword">\U0001f441</span>',
            '<app-icon *ngIf="!showConfirmPassword" name="eye" [size]="20"></app-icon>',
        )
        t = t.replace(
            '<span *ngIf="showConfirmPassword" class="eye-crossed">\U0001f441</span>',
            '<app-icon *ngIf="showConfirmPassword" name="eyeOff" [size]="20"></app-icon>',
        )
        t = t.replace(
            "<span class=\"requirement-icon\">{{ passwordRequirements.minLength ? '\u2713' : '\u2717' }}</span>",
            '<span class="requirement-icon"><app-icon [name]="passwordRequirements.minLength ? \'check\' : \'close\'" [size]="14"></app-icon></span>',
        )
        t = t.replace(
            "<span class=\"requirement-icon\">{{ passwordRequirements.hasUppercase ? '\u2713' : '\u2717' }}</span>",
            '<span class="requirement-icon"><app-icon [name]="passwordRequirements.hasUppercase ? \'check\' : \'close\'" [size]="14"></app-icon></span>',
        )
        t = t.replace(
            "<span class=\"requirement-icon\">{{ passwordRequirements.hasLowercase ? '\u2713' : '\u2717' }}</span>",
            '<span class="requirement-icon"><app-icon [name]="passwordRequirements.hasLowercase ? \'check\' : \'close\'" [size]="14"></app-icon></span>',
        )
        t = t.replace(
            "<span class=\"requirement-icon\">{{ passwordRequirements.hasDigit ? '\u2713' : '\u2717' }}</span>",
            '<span class="requirement-icon"><app-icon [name]="passwordRequirements.hasDigit ? \'check\' : \'close\'" [size]="14"></app-icon></span>',
        )
        t = t.replace(
            "<span class=\"requirement-icon\">{{ passwordRequirements.hasSpecialChar ? '\u2713' : '\u2717' }}</span>",
            '<span class="requirement-icon"><app-icon [name]="passwordRequirements.hasSpecialChar ? \'check\' : \'close\'" [size]="14"></app-icon></span>',
        )
        return t

    rw("src/app/components/register/register.component.html", reg)

    # --- email-confirmado ---
    def em(t: str) -> str:
        t = t.replace(
            '<h1 *ngIf="!isLoading && success">\u2705 Email Confirmado!</h1>',
            '<h1 *ngIf="!isLoading && success" style="display:flex;align-items:center;justify-content:center;gap:10px;"><app-icon name="check" [size]="28"></app-icon> Email Confirmado!</h1>',
        )
        t = t.replace(
            '<h1 *ngIf="!isLoading && !success">\u274c Erro ao Confirmar</h1>',
            '<h1 *ngIf="!isLoading && !success" style="display:flex;align-items:center;justify-content:center;gap:10px;"><app-icon name="close" [size]="28"></app-icon> Erro ao Confirmar</h1>',
        )
        return t

    rw("src/app/components/email-confirmado/email-confirmado.component.html", em)

    # --- profile (subset via replace) ---
    def prof(t: str) -> str:
        t = t.replace(
            '<span style="display: inline-block; transform: rotate(135deg);">\u270f</span>',
            '<app-icon name="pencil" [size]="16" style="display:inline-block"></app-icon>',
        )
        t = t.replace(
            '<span *ngIf="!fotoPerfilUrl">\U0001f464</span>',
            '<app-icon *ngIf="!fotoPerfilUrl" name="user" [size]="48"></app-icon>',
        )
        t = t.replace(
            '<span class="icon">\U0001f680</span>',
            '<app-icon name="bolt" [size]="18" class="icon"></app-icon>',
        )
        t = t.replace(
            '<span class="icon">\u2705</span>',
            '<app-icon name="check" [size]="18" class="icon"></app-icon>',
        )
        t = t.replace(
            'Usa "\u2605 Favorito" nas p\u00e1ginas',
            'Usa Favorito nas p\u00e1ginas',
        )
        t = t.replace(
            """            <button class="graph-customize-btn"
                    [class.active]="showGraphCustomizeMenu"
                    (click)="toggleGraphCustomize()"
                    title="Personalizar gr\u00e1ficos">
              \u2699
            </button>""",
            """            <button class="graph-customize-btn"
                    [class.active]="showGraphCustomizeMenu"
                    (click)="toggleGraphCustomize()"
                    title="Personalizar gr\u00e1ficos"
                    type="button">
              <app-icon name="gear" [size]="20"></app-icon>
            </button>""",
        )
        t = t.replace(
            """ <button class="conquistas-tab" 
                  [class.active]="conquistasTab === 'minhas'"
                  (click)="conquistasTab = 'minhas'">
            \U0001f3c5 Minhas Medalhas
          </button>""",
            """          <button class="conquistas-tab" 
                  [class.active]="conquistasTab === 'minhas'"
                  (click)="conquistasTab = 'minhas'"
                  type="button">
            <app-icon name="medal" [size]="18" class="tab-ico"></app-icon> Minhas Medalhas
          </button>""",
        )
        t = t.replace(
            """          <button class="conquistas-tab" 
                  [class.active]="conquistasTab === 'todas'"
                  (click)="conquistasTab = 'todas'">
            \U0001f3af Todas as Medalhas
          </button>""",
            """          <button class="conquistas-tab" 
                  [class.active]="conquistasTab === 'todas'"
                  (click)="conquistasTab = 'todas'"
                  type="button">
            <app-icon name="target" [size]="18" class="tab-ico"></app-icon> Todas as Medalhas
          </button>""",
        )
        t = t.replace(
            '<span *ngIf="!um.medalha?.iconeUrl">\U0001f3c5</span>',
            '<app-icon *ngIf="!um.medalha?.iconeUrl" name="medal" [size]="32"></app-icon>',
        )
        t = t.replace(
            '<div class="status-badge">\u2705 Conquistada</div>',
            '<div class="status-badge"><app-icon name="check" [size]="14" class="status-ico"></app-icon> Conquistada</div>',
        )
        t = t.replace(
            """  <div class="medalha-status-direita">
    <div class="status-badge">
       {{ m.conquistada ? '\u2705 Conquistada' : '\U0001f512 Por conquistar' }}
    </div>
  </div>""",
            """  <div class="medalha-status-direita">
    <div class="status-badge">
       <ng-container *ngIf="m.conquistada; else medalLockedProfileAll"><app-icon name="check" [size]="14" class="status-ico"></app-icon> Conquistada</ng-container>
       <ng-template #medalLockedProfileAll><app-icon name="lock" [size]="14" class="status-ico"></app-icon> Por conquistar</ng-template>
    </div>
  </div>""",
        )
        t = t.replace(
            '<div *ngIf="!(medal.medalha?.iconeUrl || medal.iconeUrl)" class="medal-option-placeholder">\U0001f3c5</div>',
            '<app-icon *ngIf="!(medal.medalha?.iconeUrl || medal.iconeUrl)" name="medal" [size]="40" class="medal-option-placeholder"></app-icon>',
        )
        return t

    rw("src/app/components/profile/profile.component.html", prof)

    # Fix profile if dynamic badge broke - read and fix
    pp = ROOT / "src/app/components/profile/profile.component.html"
    pt = pp.read_text(encoding="utf-8")
    if "m.conquistada ?" in pt and "medalLocked" not in pt:
        pt = pt.replace(
            """  <div class="medalha-status-direita">
    <div class="status-badge">
       {{ m.conquistada ? '�� Conquistada' : '�� Por conquistar' }}
    </div>
  </div>""",
            """  <div class="medalha-status-direita">
    <div class="status-badge">
       <ng-container *ngIf="m.conquistada; else medalLocked"><app-icon name="check" [size]="14" class="status-ico"></app-icon> Conquistada</ng-container>
       <ng-template #medalLocked><app-icon name="lock" [size]="14" class="status-ico"></app-icon> Por conquistar</ng-template>
    </div>
  </div>""",
        )
        pp.write_text(pt, encoding="utf-8")
        print("fixed profile medal status-badge")

    # --- comunidade-detalhe ---
    def com(t: str) -> str:
        t = t.replace(
            '<div class="ban-access-denied__icon" aria-hidden="true">\u26d4</div>',
            '<div class="ban-access-denied__icon" aria-hidden="true"><app-icon name="block" [size]="48"></app-icon></div>',
        )
        t = t.replace(
            '<button class="tab-btn" [class.active]="activeTab === \'ranking\'" (click)="activeTab = \'ranking\'; carregarRanking()">\U0001f3c6 Ranking</button>',
            '<button class="tab-btn" [class.active]="activeTab === \'ranking\'" (click)="activeTab = \'ranking\'; carregarRanking()" type="button"><app-icon name="trophy" [size]="16" class="tab-ico"></app-icon> Ranking</button>',
        )
        t = t.replace(
            """ <button class="metrica-btn" [class.active]="rankingMetrica === 'filmes'" (click)="mudarMetrica('filmes')">
          \U0001f3ac Filmes vistos
        </button>""",
            """        <button class="metrica-btn" [class.active]="rankingMetrica === 'filmes'" (click)="mudarMetrica('filmes')" type="button">
          <app-icon name="film" [size]="16" class="metrica-ico"></app-icon> Filmes vistos
        </button>""",
        )
        t = t.replace(
            """        <button class="metrica-btn" [class.active]="rankingMetrica === 'tempo'" (click)="mudarMetrica('tempo')">
          \u23f1\ufe0f Tempo assistido
        </button>""",
            """        <button class="metrica-btn" [class.active]="rankingMetrica === 'tempo'" (click)="mudarMetrica('tempo')" type="button">
          <app-icon name="timer" [size]="16" class="metrica-ico"></app-icon> Tempo assistido
        </button>""",
        )
        t = t.replace(
            """ <div class="ranking-pos">
            <span *ngIf="membro.posicao === 1">\U0001f947</span>
            <span *ngIf="membro.posicao === 2">\U0001f948</span>
            <span *ngIf="membro.posicao === 3">\U0001f949</span>
            <span *ngIf="membro.posicao > 3">#{{ membro.posicao }}</span>
          </div>""",
            """          <div class="ranking-pos">
            <app-icon *ngIf="membro.posicao === 1" name="crown" [size]="22"></app-icon>
            <app-icon *ngIf="membro.posicao === 2" name="trophy" [size]="22"></app-icon>
            <app-icon *ngIf="membro.posicao === 3" name="medal" [size]="22"></app-icon>
            <span *ngIf="membro.posicao > 3">#{{ membro.posicao }}</span>
          </div>""",
        )
        t = t.replace(
            """            <span *ngIf="rankingMetrica === 'filmes'" class="stat-filmes">\U0001f3ac {{ membro.filmesVistos }} filmes</span>
            <span *ngIf="rankingMetrica === 'tempo'" class="stat-tempo">\u23f1\ufe0f {{ membro.minutosAssistidos | number:'1.0-0' }} min</span>""",
            """            <span *ngIf="rankingMetrica === 'filmes'" class="stat-filmes"><app-icon name="film" [size]="14" class="stat-ico"></app-icon> {{ membro.filmesVistos }} filmes</span>
            <span *ngIf="rankingMetrica === 'tempo'" class="stat-tempo"><app-icon name="timer" [size]="14" class="stat-ico"></app-icon> {{ membro.minutosAssistidos | number:'1.0-0' }} min</span>""",
        )
        t = t.replace(
            '<span>\u26a0\ufe0f Est\u00e1s temporariamente castigado:',
            '<span><app-icon name="warning" [size]="16" class="warn-ico"></app-icon> Est\u00e1s temporariamente castigado:',
        )
        t = t.replace(
            '              <span class="search-icon" style="position:absolute; left:12px; top:50%; transform:translateY(-50%); font-size:16px; pointer-events:none;">\U0001f37f</span>',
            '              <span class="search-icon" style="position:absolute; left:12px; top:50%; transform:translateY(-50%); pointer-events:none;"><app-icon name="popcorn" [size]="16"></app-icon></span>',
        )
        t = t.replace(
            '<button type="button" class="remove-img-btn" (click)="imagemFile = null; imagemPreview = null">\u2715 Remover</button>',
            '<button type="button" class="remove-img-btn" (click)="imagemFile = null; imagemPreview = null"><app-icon name="close" [size]="14"></app-icon> Remover</button>',
        )
        t = t.replace(
            '<span *ngIf="!p.autorUserTagIconUrl" class="post-autor-tag-fallback">\U0001f3c5</span>',
            '<app-icon *ngIf="!p.autorUserTagIconUrl" name="medal" [size]="14" class="post-autor-tag-fallback"></app-icon>',
        )
        t = t.replace(
            '              \U0001f6a9 {{ p.reportsCount }}',
            '              <app-icon name="flag" [size]="14"></app-icon> {{ p.reportsCount }}',
        )
        t = t.replace(
            '<button *ngIf="p.autorId !== currentUserId && !isCurrentCastigado" class="post-action-btn report-btn" (click)="openReportModal(p)" title="Denunciar publica\u00e7\u00e3o" [disabled]="p.jaReportou">\U0001f6a9</button>',
            '<button *ngIf="p.autorId !== currentUserId && !isCurrentCastigado" class="post-action-btn report-btn" (click)="openReportModal(p)" title="Denunciar publica\u00e7\u00e3o" [disabled]="p.jaReportou" type="button"><app-icon name="flag" [size]="16"></app-icon></button>',
        )
        t = t.replace(
            '<button *ngIf="p.autorId === currentUserId && !isCurrentCastigado" class="post-action-btn edit-btn" (click)="openEditPost(p)" title="Editar">\u270f\ufe0f</button>',
            '<button *ngIf="p.autorId === currentUserId && !isCurrentCastigado" class="post-action-btn edit-btn" (click)="openEditPost(p)" title="Editar" type="button"><app-icon name="pencil" [size]="16"></app-icon></button>',
        )
        t = t.replace(
            '<button *ngIf="(p.autorId === currentUserId || isAdmin) && !isCurrentCastigado" class="post-action-btn delete-btn" (click)="openDeletePostModal(p)" title="Apagar">\U0001f5d1\ufe0f</button>',
            '<button *ngIf="(p.autorId === currentUserId || isAdmin) && !isCurrentCastigado" class="post-action-btn delete-btn" (click)="openDeletePostModal(p)" title="Apagar" type="button"><app-icon name="trash" [size]="16"></app-icon></button>',
        )
        t = t.replace(
            '                  \U0001f441\ufe0f Revelar Spoiler',
            '                  <app-icon name="eye" [size]="18"></app-icon> Revelar Spoiler',
        )
        t = t.replace(
            '              \U0001f44d <span>{{ p.likesCount || 0 }}</span>',
            '              <app-icon name="thumbsUp" [size]="16" class="vote-ico"></app-icon> <span>{{ p.likesCount || 0 }}</span>',
        )
        t = t.replace(
            '              \U0001f44e <span>{{ p.dislikesCount || 0 }}</span>',
            '              <app-icon name="thumbsDown" [size]="16" class="vote-ico"></app-icon> <span>{{ p.dislikesCount || 0 }}</span>',
        )
        for _ in range(3):
            t = t.replace(
                '<span *ngIf="!c.autorUserTagIconUrl" class="comment-autor-tag-fallback">\U0001f3c5</span>',
                '<app-icon *ngIf="!c.autorUserTagIconUrl" name="medal" [size]="14" class="comment-autor-tag-fallback"></app-icon>',
            )
 t = t.replace(
                '<span *ngIf="!m.userTagIconUrl" class="membro-tag-fallback">\U0001f3c5</span>',
                '<app-icon *ngIf="!m.userTagIconUrl" name="medal" [size]="14" class="membro-tag-fallback"></app-icon>',
            )
            t = t.replace(
                '<span *ngIf="!b.userTagIconUrl" class="membro-tag-fallback">\U0001f3c5</span>',
                '<app-icon *ngIf="!b.userTagIconUrl" name="medal" [size]="14" class="membro-tag-fallback"></app-icon>',
            )
        t = t.replace(
            """            <button class="kick-btn punish-btn" (click)="openCastigoModal(m)" title="Castigar membro">
              \u23f1\ufe0f
            </button>""",
            """            <button class="kick-btn punish-btn" (click)="openCastigoModal(m)" title="Castigar membro" type="button">
              <app-icon name="timer" [size]="18"></app-icon>
            </button>""",
        )
        t = t.replace(
            """            <button class="kick-btn ban-btn" (click)="openBanModal(m)" title="Banir membro">
              \u26d4
            </button>""",
            """            <button class="kick-btn ban-btn" (click)="openBanModal(m)" title="Banir membro" type="button">
              <app-icon name="block" [size]="18"></app-icon>
            </button>""",
        )
        t = t.replace(
            "<h3>\u270f\ufe0f Editar comunidade</h3>",
            '<h3><app-icon name="pencil" [size]="20" class="h-ico"></app-icon> Editar comunidade</h3>',
        )
        t = t.replace(
            '<button type="button" class="remove-img-btn" (click)="editBannerFile = null; editBannerPreview = null">\u2715 Remover</button>',
            '<button type="button" class="remove-img-btn" (click)="editBannerFile = null; editBannerPreview = null"><app-icon name="close" [size]="14"></app-icon> Remover</button>',
        )
        t = t.replace(
            '<button type="button" class="remove-current-img-btn" (click)="removeCurrentBanner()">\u2715 Remover</button>',
            '<button type="button" class="remove-current-img-btn" (click)="removeCurrentBanner()"><app-icon name="close" [size]="14"></app-icon> Remover</button>',
        )
        t = t.replace(
            '<button type="button" class="remove-img-btn" (click)="editIconFile = null; editIconPreview = null">\u2715 Remover</button>',
            '<button type="button" class="remove-img-btn" (click)="editIconFile = null; editIconPreview = null"><app-icon name="close" [size]="14"></app-icon> Remover</button>',
        )
        t = t.replace(
            '<button type="button" class="remove-current-img-btn" (click)="removeCurrentIcon()">\u2715 Remover</button>',
            '<button type="button" class="remove-current-img-btn" (click)="removeCurrentIcon()"><app-icon name="close" [size]="14"></app-icon> Remover</button>',
        )
        t = t.replace(
            "<h3>\u270f\ufe0f Editar publica\u00e7\u00e3o</h3>",
            '<h3><app-icon name="pencil" [size]="20" class="h-ico"></app-icon> Editar publica\u00e7\u00e3o</h3>',
        )
        t = t.replace(
            '      <span style="font-size: 32px;">\U0001f6a9</span>',
            '<app-icon name="flag" [size]="40"></app-icon>',
        )
        t = t.replace(
            '      <span style="font-size: 32px;">\u23f1\ufe0f</span>',
            '<app-icon name="timer" [size]="40"></app-icon>',
        )
        return t

    rw("src/app/components/comunidade-detalhe/comunidade-detalhe.component.html", com)

    # Fix comunidade post-autor tag - may have broken duplicate
    cp = ROOT / "src/app/components/comunidade-detalhe/comunidade-detalhe.component.html"
    ct = cp.read_text(encoding="utf-8")
    if ct.count('post-autor-tag-fallback') > 0 and 'name="flag"' in ct and 'p.autorUserTagIconUrl' in ct:
        ct = ct.replace(
            '<app-icon *ngIf="!p.autorUserTagIconUrl" name="flag" [size]="14" class="post-autor-tag-fallback"></app-icon>',
            '<app-icon *ngIf="!p.autorUserTagIconUrl" name="medal" [size]="14" class="post-autor-tag-fallback"></app-icon>',
        )
    cp.write_text(ct, encoding="utf-8")

    # --- cinema-movies ---
    def cm(t: str) -> str:
        t = t.replace(
            '<div class="hero-badge">\U0001f37f EM CARTAZ</div>',
            '<div class="hero-badge"><app-icon name="popcorn" [size]="18" class="hero-badge-ico"></app-icon> EM CARTAZ</div>',
        )
        t = t.replace(
            '      <span>\U0001f4cd</span> {{ geoError }}',
            '      <span class="geo-ico"><app-icon name="pin" [size]="18"></app-icon></span> {{ geoError }}',
        )
        t = t.replace(
            '        <span class="nearby-title">\U0001f4cd Os teus cinemas</span>',
        '        <span class="nearby-title"><app-icon name="pin" [size]="18" class="inline-ico"></app-icon> Os teus cinemas</span>',
        )
        t = t.replace(
            """          <div *ngIf="favoritosIds.has(c.id)" class="fav-badge">
            \u2b50 Favorito
          </div>""",
            """          <div *ngIf="favoritosIds.has(c.id)" class="fav-badge">
            <app-icon name="star" [size]="14" class="fav-badge-ico"></app-icon> Favorito
          </div>""",
        )
        t = t.replace(
            '            <span *ngIf="isFavorito(c)">\u2b50</span>',
            '            <span *ngIf="isFavorito(c)"><app-icon name="star" [size]="14"></app-icon></span>',
        )
        t = t.replace(
            '            <span class="badge-proximo">\U0001f4cd Cinema mais perto</span>',
            '            <span class="badge-proximo"><app-icon name="pin" [size]="14" class="inline-ico"></app-icon> Cinema mais perto</span>',
        )
        t = t.replace(
            '    <span>\u26a0\ufe0f</span> {{ error }}',
            '    <span class="warn-ico"><app-icon name="warning" [size]="20"></app-icon></span> {{ error }}',
        )
        t = t.replace(
            '    <span>\U0001f3ac</span>',
            '    <span class="big-ico"><app-icon name="film" [size]="40"></app-icon></span>',
        )
        t = t.replace(
            '        <div class="section-badge nos-badge">\U0001f3ac Cinema NOS</div>',
            '        <div class="section-badge nos-badge"><app-icon name="film" [size]="16" class="inline-ico"></app-icon> Cinema NOS</div>',
        )
        t = t.replace(
            '        <div class="section-badge city-badge">\U0001f3ac Cinema City</div>',
            '        <div class="section-badge city-badge"><app-icon name="film" [size]="16" class="inline-ico"></app-icon> Cinema City</div>',
        )
        t = t.replace(
            '                      <span class="closest-icon">\U0001f4cd</span>',
            '                      <span class="closest-icon"><app-icon name="pin" [size]="14"></app-icon></span>',
        )
        return t

    rw("src/app/components/cinema-movies/cinema-movies.component.html", cm)

    # duplicate closest-icon replace_all
    p_cm = ROOT / "src/app/components/cinema-movies/cinema-movies.component.html"
    cmt = p_cm.read_text(encoding="utf-8")
    cmt = cmt.replace(
        '<span class="closest-icon"><app-icon name="pin" [size]="14"></app-icon></span>',
        '<span class="closest-icon"><app-icon name="pin" [size]="14"></app-icon></span>',
    )
    # replace remaining� in closest-icon if any
    cmt = cmt.replace(
        '<span class="closest-icon">\U0001f4cd</span>',
        '<span class="closest-icon"><app-icon name="pin" [size]="14"></app-icon></span>',
    )
    p_cm.write_text(cmt, encoding="utf-8")

    # --- cinema-map ---
    def cmap(t: str) -> str:
        t = t.replace(
            '<div class="hero-badge">\U0001f4cd LOCALIZA\u00c7\u00c3O</div>',
            '<div class="hero-badge"><app-icon name="map" [size]="18" class="hero-badge-ico"></app-icon> LOCALIZA\u00c7\u00c3O</div>',
        )
        t = t.replace(
            '    <span>\u26a0\ufe0f</span> {{ geoError }}',
            '    <span class="warn-ico"><app-icon name="warning" [size]="20"></app-icon></span> {{ geoError }}',
        )
        t = t.replace(
            '    <span>\u26a0\ufe0f</span> N\u00e3o foi poss\u00edvel carregar',
            '    <span class="warn-ico"><app-icon name="warning" [size]="20"></app-icon></span> N\u00e3o foi poss\u00edvel carregar',
        )
        t = t.replace(
            '    <div class="map-overlay-label">\U0001f5fa\ufe0f Mapa interativo</div>',
            '    <div class="map-overlay-label"><app-icon name="map" [size]="18" class="inline-ico"></app-icon> Mapa interativo</div>',
        )
        t = t.replace(
            '      \u2b50 Clica na estrela para guardar um favorito &nbsp;|&nbsp; \U0001f3ac Clica no nome para ver no mapa',
            '      <app-icon name="star" [size]="14" class="inline-ico"></app-icon> Clica na estrela para guardar um favorito &nbsp;|&nbsp; <app-icon name="film" [size]="14" class="inline-ico"></app-icon> Clica no nome para ver no mapa',
        )
        t = t.replace(
            '        <div class="cinema-icon">\U0001f3ac</div>',
            '        <div class="cinema-icon"><app-icon name="film" [size]="22"></app-icon></div>',
        )
        t = t.replace(
            """          <span *ngIf="togglingId !== cinemaId(c)">{{ isFavorito(c) ? '\u2b50' : '\u2606' }}</span>""",
            """          <span *ngIf="togglingId !== cinemaId(c)"><app-icon name="star" [size]="18" [style.opacity]="isFavorito(c) ? 1 : 0.35"></app-icon></span>""",
        )
        t = t.replace(
            '          \U0001f3ac\n </a>',
            '          <app-icon name="film" [size]="18"></app-icon>\n        </a>',
        )
        return t

    rw("src/app/components/cinema-map/cinema-map.component.html", cmap)

    # --- movie-page ---
    def mp(t: str) -> str:
        t = t.replace(
            '<button class="trailer-close" (click)="closeTrailer()">\u2715</button>',
            '<button class="trailer-close" (click)="closeTrailer()" type="button" aria-label="Fechar"><app-icon name="close" [size]="22"></app-icon></button>',
        )
        t = t.replace(
            '<span *ngIf="inWatchLater">\u2714 Quero Ver</span>',
            '<span *ngIf="inWatchLater"><app-icon name="check" [size]="16" class="btn-ico"></app-icon> Quero Ver</span>',
        )
        t = t.replace(
            '<span *ngIf="inWatched">\u2714 J\u00e1 Vi</span>',
            '<span *ngIf="inWatched"><app-icon name="check" [size]="16" class="btn-ico"></app-icon> J\u00e1 Vi</span>',
        )
        t = t.replace(
            """            <span *ngIf="!isFavorite">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor" style="vertical-align: middle; margin-right: 4px;">
                <path d="M22 9.24l-7.19-.62L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21 12 17.27 18.18 21l-1.63-7.03L22 9.24zM12 15.4l-3.76 2.27 1-4.28-3.32-2.88 4.38-.38L12 6.1l1.71 4.04 4.38.38-3.32 2.88 1 4.28L12 15.4z" />
              </svg>
              Favorito
            </span>""",
            """            <span *ngIf="!isFavorite">
              <app-icon name="star" [size]="16" class="btn-ico"></app-icon>
              Favorito
            </span>""",
        )
        t = t.replace(
            """            <span *ngIf="isFavorite">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor" style="vertical-align: middle; margin-right: 4px;">
                <path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z" />
              </svg>
              Favoritos
            </span>""",
            """            <span *ngIf="isFavorite">
              <app-icon name="star" [size]="16" class="btn-ico"></app-icon>
              Favoritos
            </span>""",
        )
        t = t.replace(
            """                      [class.half]="isStarHalf(i)">\u2605</span>
              </button>""",
            """                      [class.half]="isStarHalf(i)"><app-icon name="star" [size]="22" class="our-star-glyph"></app-icon></span>
              </button>""",
        )
        t = t.replace(
            '<span *ngIf="!c.userTagIconUrl" class="user-tag-fallback">\U0001f3c5</span>',
            '<app-icon *ngIf="!c.userTagIconUrl" name="medal" [size]="14" class="user-tag-fallback"></app-icon>',
        )
        t = t.replace(
            '                      \U0001f44d {{ c.likeCount || 0 }}',
            '                      <app-icon name="thumbsUp" [size]="16" class="vote-ico"></app-icon> {{ c.likeCount || 0 }}',
        )
        t = t.replace(
            '                      \U0001f44e {{ c.dislikeCount || 0 }}',
            '                      <app-icon name="thumbsDown" [size]="16" class="vote-ico"></app-icon> {{ c.dislikeCount || 0 }}',
        )
        return t

    rw("src/app/components/movie-page/movie-page.component.html", mp)

    # --- selecionar-generos ---
    rw(
        "src/app/components/selecionar-generos/selecionar-generos.component.html",
        lambda t: t.replace(
            "o algoritmo j\u00e1 est\u00e1 a preparar recomenda\u00e7\u00f5es \U0001f50d\U0001f3ac",
            "o algoritmo j\u00e1 est\u00e1 a preparar recomenda\u00e7\u00f5es <app-icon name=\"search\" [size]=\"14\" class=\"inline-ico\"></app-icon> <app-icon name=\"film\" [size]=\"14\" class=\"inline-ico\"></app-icon>",
        ),
    )

    # --- cypress ---
    rw(
        "cypress/e2e/community.cy.ts",
        lambda t: t.replace(
            """    cy.get('.ranking-row').eq(0).should('contain', '\U0001f947').and('contain', 'cinefiloPro').and('contain', '150 filmes');
    cy.get('.ranking-row').eq(1).should('contain', '\U0001f948').and('contain', 'moviebuff').and('contain', '120 filmes');
    cy.get('.ranking-row').eq(2).should('contain', '\U0001f949').and('contain', 'testuser').and('contain', 'Tu').and('contain', '80 filmes');
    cy.get('.ranking-row').eq(3).should('contain', '#4').and('contain', 'filmlover').and('contain', '50 filmes');
    cy.get('.ranking-row').eq(4).should('contain', '#5').and('contain', 'casualviewer').and('contain', '20 filmes');""",
            """    cy.get('.ranking-row.top1').should('contain', 'cinefiloPro').and('contain', '150 filmes');
    cy.get('.ranking-row.top2').should('contain', 'moviebuff').and('contain', '120 filmes');
    cy.get('.ranking-row.top3').should('contain', 'testuser').and('contain', 'Tu').and('contain', '80 filmes');
    cy.get('.ranking-row').eq(3).should('contain', '#4').and('contain', 'filmlover').and('contain', '50 filmes');
    cy.get('.ranking-row').eq(4).should('contain', '#5').and('contain', 'casualviewer').and('contain', '20 filmes');""",
        ).replace(
            """    cy.get('.ranking-row').eq(0).should('contain', '\U0001f947').and('contain', 'cinefiloPro').and('contain', '25.000 min');
    cy.get('.ranking-row').eq(1).should('contain', '\U0001f948').and('contain', 'moviebuff').and('contain', '20.000 min');
    cy.get('.ranking-row').eq(2).should('contain', '\U0001f949').and('contain', 'filmlover').and('contain', '12.000 min');
    cy.get('.ranking-row').eq(3).should('contain', '#4').and('contain', 'testuser').and('contain', 'Tu').and('contain', '10.000 min');
    cy.get('.ranking-row').eq(4).should('contain', '#5').and('contain', 'casualviewer').and('contain', '3.000 min');""",
            """    cy.get('.ranking-row.top1').should('contain', 'cinefiloPro').and('contain', '25.000 min');
    cy.get('.ranking-row.top2').should('contain', 'moviebuff').and('contain', '20.000 min');
    cy.get('.ranking-row.top3').should('contain', 'filmlover').and('contain', '12.000 min');
    cy.get('.ranking-row').eq(3).should('contain', '#4').and('contain', 'testuser').and('contain', 'Tu').and('contain', '10.000 min');
    cy.get('.ranking-row').eq(4).should('contain', '#5').and('contain', 'casualviewer').and('contain', '3.000 min');""",
        ),
    )

    rw(
        "cypress/e2e/higher-or-lower.cy.ts",
        lambda t: t.replace(
            "cy.contains('.hi-badge', '\U0001f3ac Filmes').should('be.visible');",
            "cy.contains('.hi-badge', 'Filmes').should('be.visible');",
        ),
    )

    print("done")


if __name__ == "__main__":
    main()
