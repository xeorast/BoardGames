import { Component, Input, OnInit } from '@angular/core';
import { Game } from '../logic/game';
import { AeFieldModel } from './ae-feld-model';

@Component( {
  selector: 'app-alea-evangelii-board',
  templateUrl: './ae-board.component.html',
  styleUrls: ['./ae-board.component.scss']
} )
export class AeBoardComponent implements OnInit {

  private _game?: Game | null;
  private _fields?: AeFieldModel[];

  @Input()
  public get game(): Game | undefined | null { return this._game; }
  public set game( value: Game | undefined | null ) {
    this._game = value;
    if ( value ) {
      this._fields = this.generateFields( value )
    }
  }
  public get fields() { return this._fields }

  public get gameResult(): GameResult | null {
    if ( !this.game?.winner ) {
      return null
    }

    return this.game.winner == this.game.playingAs ? "Win" : "Loss"
  }

  public get isWin() {
    return this.gameResult == 'Win'
  }

  constructor() { }

  ngOnInit(): void {
  }

  generateFields( game: Game ) {
    let fields: AeFieldModel[] = []
    for ( let row = 0; row < game.board.height; row++ ) {
      for ( let column = 0; column < game.board.width; column++ ) {
        fields.push( new AeFieldModel( game, { row, column } ) )
      }
    }
    return fields
  }

  public get placeholderFields() { return AeFieldModel.placeholderFields }

}

export type GameResult = "Loss" | "Win"
